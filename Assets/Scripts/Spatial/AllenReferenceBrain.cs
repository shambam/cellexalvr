using System;
using System.Collections;
using System.Collections.Generic;
using CellexalVR.DesktopUI;
using Spatial;
using UnityEngine;

public class AllenReferenceBrain : MonoBehaviour
{
    public GameObject rootModel;
    public GameObject models;
    public Dictionary<int, SpatialReferenceModel> idToModelDictionary = new Dictionary<int, SpatialReferenceModel>();

    [HideInInspector] public Vector3 startPosition;
    [HideInInspector] public Vector3 startScale;
    [HideInInspector] public Quaternion startRotation;

    // private Dictionary<int, Tuple<string, Color> idToNameDictionary = new Dictionary<int, Tuple<string, Color>();

    private void Start()
    {
        startPosition = new Vector3(-0.015f, 0f, 0.25f);
        startScale = new Vector3(0.82f, 0.82f, 0.82f);
        startRotation = Quaternion.Euler(0, 90, -180);
        foreach (Transform model in models.transform)
        {
            idToModelDictionary[int.Parse(model.gameObject.name)] = model.GetComponent<SpatialReferenceModel>();
        }
    }


    [ConsoleCommand("brainModel", aliases: new string[] {"spawnbrainmodel", "sbm"})]
    public void SpawnModel(int id)
    {
        idToModelDictionary.TryGetValue(id, out SpatialReferenceModel objToSpawn);
        Instantiate(objToSpawn, transform);
        objToSpawn.id = id;
    }

    private void Update()
    {
    }
}