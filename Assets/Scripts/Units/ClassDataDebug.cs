using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClassDataDebug : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

        // Assuming you have a reference to your ClassDataManager
        ClassDataManager manager = GetComponent<ClassDataManager>();

        // Get class data by index
        UnitClass classData = manager.GetClassDataByIndex(0);
        if (classData != null)
        {
            Debug.Log($"Class at index 0: {classData.name}");
        }

        // Get class data by name
        UnitClass warrior = manager.GetClassDataByName("Fighter");
        if (warrior != null)
        {
            Debug.Log($"Warrior base HP: {warrior.baseHP}");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
