﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HighlightSpot : MonoBehaviour {

    public TutorialManager tutorialManager;
    
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {

	}

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Graph")
        {
            this.gameObject.GetComponent<Collider>().enabled = false;
            foreach (ParticleSystem sys in this.GetComponentsInChildren<ParticleSystem>())
            {
                sys.Stop();
            }
            tutorialManager.NextStep();
        }
        if ((other.tag == "Controller" || other.tag == "Cells") && !this.name.Contains("HighlightSpot"))
        {
            foreach (ParticleSystem sys in this.GetComponentsInChildren<ParticleSystem>())
            {
                sys.Stop();
            }
            if (this.name == "Portal")
            {
                SceneManager.LoadScene("SceneLoaderTest");
            }
            //this.gameObject.SetActive(false);
        }

    }
}
