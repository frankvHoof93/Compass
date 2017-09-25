using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DirectionTest : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        if (Input.GetKey(KeyCode.F1))
            Debug.Log("AAAAAAAA");
        if (Input.GetKeyDown(KeyCode.W))
            transform.position += Vector3.forward;
        if (Input.GetKeyDown(KeyCode.S))
            transform.position -= transform.forward;
        if (Input.GetKeyDown(KeyCode.A))
            transform.position -= transform.right;
        if (Input.GetKeyDown(KeyCode.D))
            transform.position += transform.right;
        if (Input.GetKeyDown(KeyCode.Q))
            transform.position -= transform.up;
        if (Input.GetKeyDown(KeyCode.E))
            transform.position += transform.up;
    }
}
