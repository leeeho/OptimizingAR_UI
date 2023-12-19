using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;

[System.Serializable]
public class ButtonGroupMaterials
{
    public List<Material> materials = new List<Material>();
}

public class PopupRandom : MonoBehaviour
{
    public List<Transform> buttonSizeGroups = new List<Transform>();
    public List<ButtonGroupMaterials> buttonGroupMaterials = new List<ButtonGroupMaterials>();
    public GameObject[] popUp = new GameObject[5];
    public GameObject colliderHand;
    public GameObject targetObject;

    private int counter;
    private int currentPopUpIndex = -1;
    private int currentButtonSizeGroupIndex = -1;
    private float spaceBarPressTime;
    private float collisionTime;
    private bool showPopUpAndRecordTime = false;
    private bool collided = false;

    private List<Vector3> recordedPositions = new List<Vector3>();
    private List<string> collidedObjectNames = new List<string>();
    private List<float> timeDifferences = new List<float>();

    public List<Material> GetShuffledMaterialsForGroup(int groupIndex)
    {
        if (groupIndex >= 0 && groupIndex < buttonGroupMaterials.Count)
        {
            return buttonGroupMaterials[groupIndex].materials;
        }
        else
        {
            UnityEngine.Debug.LogError("Invalid group index.");
            return new List<Material>();
        }
    }

    void Start()
    {
        HideAllGroups();
        ShuffleGroupsOrder();
    }

    void Update()
    {
        // Check if the target object is not null and the colliderHand is assigned
        if (targetObject != null && colliderHand != null)
        {
            // Set the colliderHand's position to match the target object's position
            colliderHand.transform.position = targetObject.transform.position;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            counter++;
            spaceBarPressTime = Time.time;
            showPopUpAndRecordTime = true;

            recordedPositions.Clear();
            collidedObjectNames.Clear();
            timeDifferences.Clear();

            ShuffleMaterialsInGroups();
            ShowRandomPopUp();
            ShowRandomGroup();

            if (counter < 6)
                UnityEngine.Debug.Log("trial" + counter);
        }

        if (showPopUpAndRecordTime)
        {
            recordedPositions.Add(colliderHand.transform.position);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Check if the colliderHand triggers any object with the 'collider' tag
        if (other.gameObject.CompareTag("collider"))
        {
            collisionTime = Time.time;

            // Record the position only if the flag is set
            if (showPopUpAndRecordTime)
            {
                float timeDifference = collisionTime - spaceBarPressTime;
                UnityEngine.Debug.Log("Time: " + timeDifference + " seconds");
                UnityEngine.Debug.Log("Collided with object: " + other.gameObject.name);

                timeDifferences.Add(timeDifference);
                collidedObjectNames.Add(other.gameObject.name);

                SaveRecordedPositionToCSV(); // Save the CSV here

                showPopUpAndRecordTime = false; // Reset the flag
                collided = true; // Set the collided flag
            }
        }
        else
        {
            // Check if the colliderHand triggers any object in buttonSizeGroups
            foreach (Transform buttonSizeGroup in buttonSizeGroups)
            {
                if (other.transform.IsChildOf(buttonSizeGroup))
                {
                    UnityEngine.Debug.Log("Collided with a buttonSizeGroup!");

                    // Hide the specific group when colliderHand triggers an object in the group
                    HideGroup(buttonSizeGroup);

                    return; // Exit the loop after hiding the first matching group
                }
            }
        }
    }

    void LateUpdate()
    {
        // LateUpdate is called after all Update functions have been called.
        // Check if a collision has occurred, and if so, hide the groups and popups.
        if (collided)
        {
            HideAllGroups();
            collided = false; // Reset the collided flag
        }
    }

    void ShuffleGroupsOrder()
    {
        int n = buttonSizeGroups.Count;
        for (int i = 0; i < n - 1; i++)
        {
            int j = UnityEngine.Random.Range(i, n);
            SwapTransforms(buttonSizeGroups, i, j);
            SwapMaterials(buttonGroupMaterials, i, j);
        }
    }

    void SwapTransforms(List<Transform> list, int indexA, int indexB)
    {
        Transform temp = list[indexA];
        list[indexA] = list[indexB];
        list[indexB] = temp;
    }

    void SwapMaterials(List<ButtonGroupMaterials> list, int indexA, int indexB)
    {
        ButtonGroupMaterials temp = list[indexA];
        list[indexA] = list[indexB];
        list[indexB] = temp;
    }

    void ShuffleMaterialsInGroups()
    {
        foreach (ButtonGroupMaterials bgm in buttonGroupMaterials)
        {
            ShuffleMaterials(bgm.materials);
        }
    }

    void ShuffleMaterials(List<Material> materials)
    {
        int n = materials.Count;
        for (int i = 0; i < n; i++)
        {
            int j = UnityEngine.Random.Range(i, n);
            Material temp = materials[i];
            materials[i] = materials[j];
            materials[j] = temp;
        }
    }

    void ShowRandomPopUp()
    {
        if (counter < 6)
        {
            if (currentPopUpIndex != -1)
            {
                popUp[currentPopUpIndex].SetActive(false);
            }
            currentPopUpIndex = counter - 1;

            if (currentPopUpIndex < popUp.Length)
            {
                popUp[currentPopUpIndex].SetActive(true);
            }
        }
        else
        {
            for (int i = 0; i < 6; i++)
            {
                popUp[i].SetActive(false);
            }
        }
    }

    void ShowRandomGroup()
    {
        if (currentButtonSizeGroupIndex != -1)
        {
            HideGroup(buttonSizeGroups[currentButtonSizeGroupIndex]);
        }
        currentButtonSizeGroupIndex = counter - 1;
        if (currentButtonSizeGroupIndex < buttonSizeGroups.Count)
        {
            ShowGroup(buttonSizeGroups[currentButtonSizeGroupIndex]);
        }
        if (counter >= 6)
        {
            HideGroup(buttonSizeGroups[currentButtonSizeGroupIndex]);
        }
    }

    void ShowGroup(Transform buttonSizeGroup)
    {
        buttonSizeGroup.gameObject.SetActive(true);

        Renderer[] planeRenderers = buttonSizeGroup.GetComponentsInChildren<Renderer>().Where(r => r.CompareTag("Plane")).ToArray();

        if (planeRenderers.Length == buttonGroupMaterials[currentButtonSizeGroupIndex].materials.Count)
        {
            List<Material> shuffledMaterials = buttonGroupMaterials[currentButtonSizeGroupIndex].materials.OrderBy(x => UnityEngine.Random.value).ToList();

            for (int i = 0; i < planeRenderers.Length; i++)
            {
                planeRenderers[i].material = shuffledMaterials[i];
            }
        }
        else
        {
            UnityEngine.Debug.LogError("Number of materials does not match the number of planes in the group.");
        }
    }

    void HideGroup(Transform buttonSizeGroup)
    {
        buttonSizeGroup.gameObject.SetActive(false);
    }

    void HideAllGroups()
    {
        foreach (Transform buttonSizeGroup in buttonSizeGroups)
        {
            buttonSizeGroup.gameObject.SetActive(false);
        }

        foreach (var popupObject in popUp)
        {
            popupObject.SetActive(false);
        }
    }

    void SaveRecordedPositionToCSV()
    {
        string filePath = "Assets/Ergonomics/P1/" + counter + "/RecordedPositions.csv";

        using (StreamWriter writer = new StreamWriter(filePath))
        {
            // Write header
            writer.WriteLine("PositionX,PositionY,PositionZ");

            // Write data
            foreach (Vector3 position in recordedPositions)
            {
                writer.WriteLine($"{position.x},{position.y},{position.z}");
            }

            UnityEngine.Debug.Log("Recorded data saved to CSV.");
        }
    }
}
