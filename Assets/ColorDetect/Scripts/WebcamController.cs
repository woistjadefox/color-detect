using UnityEngine;
using System.Collections;

public class WebcamController : MonoBehaviour {

    public ColorDetect colorDetection;
    public float speedMultiplayer = 1f;

    private Vector3 newPos; 

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {

        newPos = Camera.main.ScreenToWorldPoint(new Vector3(colorDetection.colorPosition.x, colorDetection.colorPosition.y, Camera.main.nearClipPlane));
        
        newPos.z = 0;
        newPos.x = Mathf.Lerp(transform.position.x, newPos.x, 1f * Time.deltaTime * speedMultiplayer);
        newPos.y = Mathf.Lerp(transform.position.y, newPos.y, 1f * Time.deltaTime * speedMultiplayer);
        
        transform.position = newPos;
	}
}
