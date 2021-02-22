using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToWeb : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}

    public void btnGoToWeb()
    {
        Application.OpenURL("https://computermuseum.uwaterloo.ca/");
    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
