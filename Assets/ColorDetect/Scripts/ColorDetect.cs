using UnityEngine;
using System.Collections;
using System.Threading;

public class ColorHSV {
	public float hue = 0.0f;
	public int saturation = 0;
	public int value = 0;
	public ColorHSV() {}
	public ColorHSV(float inHue, int inSat, int inVal) {
		hue = inHue;
		saturation = inSat;
		value = inVal;
	}
}

public class ThresholdHSVPreset {
	public ColorHSV lowBound;
	public ColorHSV highBound;
	public ThresholdHSVPreset(ColorHSV inLowBound, ColorHSV inHighBound) {
		lowBound = inLowBound;
		highBound = inHighBound;
	}
}

public class ColorDetect : MonoBehaviour {


	//tracking frquency
	public float stepSize = 0.1f;
	private float timeSinceLastStep = 0.0f;
	
	//color traking
	private WebCamTexture webcamTexture;
	private Texture2D targetTexture;
	private Color32[] img_src;
	private Color32[] img_end;
	private ColorHSV px_color_hsv;
	
	//center calc
	private int imgWidth;
	private int imgHeight;
	private int[] hWeights;
	private int[] vWeights;
	private int hMaxWeight;
	private int vMaxWeight;
	private int hMaxWeightInt;
	private int vMaxWeightInt;

    private ColorHSV _hsvColor;
	
	//display
	public GameObject calibScreen;
	
	//color
	private ColorHSV threshold_low;
	private ColorHSV threshold_high;
	
	//output
	private Vector2 newPosition;
    private Vector2 lastPosition;
    public float distanceTolerance = 0.5f;
    public Vector2 colorPosition;
	
	//gui
	private PresetsConfigStorage presetsStorage;
	private bool showCalibration = false;
	private GUIContent[] comboBoxList;
	private ComboBox comboBoxControl;
	private int comboBoxElement = 0;
	private int comboBoxElementNew = 0;
	private GUIStyle listStyle = new GUIStyle();
	private Color imgHighlightColor = Color.magenta;

    private bool newWebcamData = false;
    private Thread calcThread;
    private bool lockCalc = false;
	
    public bool debugOutput = false;
	
	public void Start () {

		//generate new webcamtexture
		webcamTexture = new WebCamTexture();
        webcamTexture.Play();
		imgWidth = webcamTexture.width;
		imgHeight = webcamTexture.height;
		targetTexture = new Texture2D(imgWidth, imgHeight);
		
		//prepare data
		img_src = new Color32[imgWidth * imgHeight];
		img_end = new Color32[imgWidth * imgHeight];
		hWeights = new int[imgHeight];
		vWeights = new int[imgWidth];
		
		//set materials
		calibScreen.renderer.material.mainTexture = targetTexture;
		
		if (debugOutput) {
			Debug.Log("Screensize - width: " + imgWidth	+ " / height: " + imgHeight);
		}
		
		//***GUI***//
		presetsStorage = new PresetsConfigStorage();
		
		//combobox/presetlist
		comboBoxList = new GUIContent[presetsStorage.getPresetCount()];
		for (int i = 0; i< comboBoxList.Length; i++) {
			comboBoxList[i] = new GUIContent(presetsStorage.getNameAtPosition(i));
		}
		
		listStyle.normal.textColor = Color.white; 
		listStyle.onHover.background =
		listStyle.hover.background = new Texture2D(2, 2);
		listStyle.padding.left =
		listStyle.padding.right =
 		listStyle.padding.top =
		listStyle.padding.bottom = 4;
 		
		//init defaults
		comboBoxControl = new ComboBox(new Rect(0, 30, 150, 20), comboBoxList[comboBoxElement], comboBoxList, "button", "box", listStyle);
		threshold_low = presetsStorage.getPresetAtPos(comboBoxElementNew).lowBound;
		threshold_high = presetsStorage.getPresetAtPos(comboBoxElementNew).highBound;

        this.StartCalcThread();
        
	}

    void StartCalcThread() {

        calcThread = new Thread(() => {

            while (true) {

                if (lockCalc) {

                    this.doCalc();
                    this.resetWeights();
                    lockCalc = false;
                }


                Thread.Sleep(10);
            }

        });

        calcThread.Priority = System.Threading.ThreadPriority.Lowest;
        calcThread.Start();
    }

    void Update() {

        /*
        //increase time counter
        timeSinceLastStep += Time.deltaTime;
        if (timeSinceLastStep > stepSize) {
            //executes the tracking
            //this.doColorTracking();

            if (!this.newWebcamData) {
                this.doColorTracking();
            }

            //reset counter
            timeSinceLastStep = 0.0f;
        }
        */

        this.doColorTracking();



        //fetch user inputs
        this.getUserInput();
    }
	
	private void doColorTracking() {

        if (lockCalc) {
            return;
        }


		//capture data
        webcamTexture.GetPixels32(img_src);

        //this.doCalc();
		
		//set position of cursor with dampening
        /*
		if (Vector3.Distance(cursor.transform.position, newPosition) > 0.05f) {


            newPosition.z = cursor.transform.position.z;

            cursor.transform.position = Vector3.Lerp(cursor.transform.position, newPosition, Time.deltaTime * this.cursorSpeed);

			//add data to rehabconnex
			//Debug.Log("X:" + ((float)(imgWidth-vMaxWeightInt)/imgWidth).ToString() + " / Y: " + ((float)(imgHeight-hMaxWeightInt)/imgHeight).ToString());
		}
        */
		
		//set display data
		if (showCalibration) {
			targetTexture.SetPixels32(img_end);
			targetTexture.Apply();
		}
		
		//cleanup
		//this.resetWeights();

        lockCalc = true;
	}

    private void doCalc() {

        int j = 0;
        for (int i = 0; i < img_src.Length; i++) {

            //convert pixel to hsv
            px_color_hsv = this.RGBToHSV(img_src[i]);

            //apply threshold
            if (px_color_hsv.hue >= threshold_low.hue && px_color_hsv.hue <= threshold_high.hue &&
                px_color_hsv.saturation >= threshold_low.saturation && px_color_hsv.saturation <= threshold_high.saturation &&
                px_color_hsv.value >= threshold_low.value && px_color_hsv.value <= threshold_high.value) {

                if (showCalibration) {
                    //set color
                    img_end[i] = imgHighlightColor;
                }

                //increase weights
                vWeights[i % imgWidth] += 1;
                hWeights[j] += 1;
            } else {
                if (showCalibration) {
                    //set color
                    img_end[i] = img_src[i];
                }
            }

            //directly evaluate horizontal max weight
            if (i % imgWidth == imgWidth - 1) {
                if (hMaxWeight < hWeights[j]) {
                    hMaxWeight = hWeights[j];
                    hMaxWeightInt = j;
                }
                j++;
            }

            //TODO: image optimisatation through closing: dilate, then erode
        }

        //point selection
        for (int i = 0; i < vWeights.Length; i++) {
            if (vMaxWeight < vWeights[i]) {
                vMaxWeight = vWeights[i];
                vMaxWeightInt = i;
            }
        }

        //calc position (need to inverte x)
        if (vMaxWeightInt != 0 && hMaxWeightInt != 0) {
            
            newPosition.x = ((float)(imgWidth - vMaxWeightInt) / imgWidth) * Screen.width;
            newPosition.y = ((float)hMaxWeightInt / imgHeight) * Screen.height;

            if ((lastPosition - newPosition).magnitude > distanceTolerance) {

                colorPosition = newPosition;
                lastPosition = colorPosition;
            }
        }

    }

    private void resetWeights() {

        hMaxWeight = 0;
        vMaxWeight = 0;
        hMaxWeightInt = 0;
        vMaxWeightInt = 0;


        for (int i = 0; i < hWeights.Length; i++) {
            hWeights[i] = 0;
        }

        for (int i = 0; i < vWeights.Length; i++) {
            vWeights[i] = 0;
        }

    }
	
	private ColorHSV RGBToHSV(Color32 inColor) {

		_hsvColor = new ColorHSV();
				
		float min = Mathf.Min(Mathf.Min(inColor.r, inColor.g), inColor.b);
		float max = Mathf.Max(Mathf.Max(inColor.r, inColor.g), inColor.b); 
		float chroma = max - min;
		
		//If Chroma is 0, then S is 0 by definition, and H is undefined but 0 by convention.
		if(chroma != 0.0f) {
			if(inColor.r == max) {
				_hsvColor.hue = (inColor.g - inColor.b) / chroma;
	 
				if(_hsvColor.hue < 0.0f) {
					_hsvColor.hue += 6.0f;
				}
			} else if(inColor.g == max) {
				_hsvColor.hue = ((inColor.b - inColor.r) / chroma) + 2.0f;
			} else {
				_hsvColor.hue = ((inColor.r - inColor.g) / chroma) + 4.0f;
			}
	 
			_hsvColor.hue *= 60.0f;
			_hsvColor.saturation = Mathf.RoundToInt(chroma/max*100.0f);
		}
	 
		_hsvColor.value = Mathf.RoundToInt(max/255.0f*100.0f);
 
		//Debug.Log("Hue: "+ hsvColor.hue + " / Sat: " + hsvColor.saturation + "% / Val: " + hsvColor.value + "%");
		
		return _hsvColor;
	}
	
	private void getUserInput() {
		//open/close calibration
		if (Input.GetKeyDown(KeyCode.C)) {
			if (showCalibration) {
				this.closeCalibration();
			} else {
				this.openCalibration();
			}
		}
	}
	
	private void closeCalibration() {
		showCalibration = false;
		calibScreen.SetActive(false);
	}
	
	private void openCalibration() {
		showCalibration = true;
		calibScreen.SetActive(true);
	}

    public void OnGUI() {
        if (showCalibration) {
            GUILayout.BeginArea(new Rect(10, 30, 300, 200));
            GUILayout.Label("Threshold Color Calibration (HSV Space)");
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            GUILayout.BeginVertical();
            GUILayout.Label("");
            GUILayout.Label(threshold_low.hue + "");
            GUILayout.Space(2);
            GUILayout.Label(threshold_low.saturation + "%");
            GUILayout.Space(2);
            GUILayout.Label(threshold_low.value + "%");
            GUILayout.EndVertical();
            GUILayout.Space(10);
            GUILayout.BeginVertical();
            GUILayout.Label("MIN");
            GUILayout.Space(5);
            threshold_low.hue = (int)GUILayout.HorizontalSlider(threshold_low.hue, 0.0f, 360.0f);
            GUILayout.Space(11);
            threshold_low.saturation = (int)GUILayout.HorizontalSlider(threshold_low.saturation, 0.0f, 100.0f);
            GUILayout.Space(11);
            threshold_low.value = (int)GUILayout.HorizontalSlider(threshold_low.value, 0.0f, 100.0f);
            GUILayout.EndVertical();
            GUILayout.Space(10);
            GUILayout.BeginVertical();
            GUILayout.Label("MAX");
            GUILayout.Space(5);
            threshold_high.hue = (int)GUILayout.HorizontalSlider(threshold_high.hue, 0.0f, 360.0f);
            GUILayout.Space(11);
            threshold_high.saturation = (int)GUILayout.HorizontalSlider(threshold_high.saturation, 0.0f, 100.0f);
            GUILayout.Space(11);
            threshold_high.value = (int)GUILayout.HorizontalSlider(threshold_high.value, 0.0f, 100.0f);
            GUILayout.EndVertical();
            GUILayout.Space(10);
            GUILayout.BeginVertical();
            GUILayout.Label("");
            GUILayout.Label(threshold_high.hue + "");
            GUILayout.Space(2);
            GUILayout.Label(threshold_high.saturation + "%");
            GUILayout.Space(2);
            GUILayout.Label(threshold_high.value + "%");
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            GUILayout.BeginArea(new Rect(30, 180, 200, 200));
            GUILayout.Label("Presets");
            comboBoxElementNew = comboBoxControl.Show();
            GUILayout.EndArea();

            GUILayout.BeginArea(new Rect(300, 280, 180, 50));
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("SAVE")) {
                presetsStorage.savePreset(new ThresholdHSVPreset(threshold_low, threshold_high));
            }
            if (GUILayout.Button("SAVE AS NEW")) {
                //TODO
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("OK & CLOSE")) {
                this.closeCalibration();
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.EndArea();

            //check for combobox change
            if (comboBoxElementNew != comboBoxElement) {
                //change settings
                threshold_low = presetsStorage.getPresetAtPos(comboBoxElementNew).lowBound;
                threshold_high = presetsStorage.getPresetAtPos(comboBoxElementNew).highBound;
                comboBoxElement = comboBoxElementNew;
            }
        }
    }

    void OnDisable() {
        calcThread.Abort();
    }

    void OnApplicationQuit() {
        calcThread.Abort();
    }

    void OnDestroy() {
        calcThread.Abort();
        webcamTexture.Stop();
    }
}