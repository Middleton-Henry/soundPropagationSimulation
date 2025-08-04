using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.IO;

public class computeManager : MonoBehaviour
{
    private int renderCount = 1;

    [Header("Compute Shader")]
    public ComputeShader airSimulationCompute;
    private ComputeBuffer  airSimulaterBuffer;
    public int bufferResolution;

    int bufferMemory;

    public RenderTexture computeResult;

    [Header("Render Textures")]
    public RenderTexture directionResultPositiveX;
    public RenderTexture directionResultNegativeX;
    public RenderTexture directionResultPositiveY;
    public RenderTexture directionResultNegativeY;

    public RenderTexture directionResultPositiveXPositiveY;
    public RenderTexture directionResultNegativeXPositiveY;
    public RenderTexture directionResultPositiveXNegativeY;
    public RenderTexture directionResultNegativeXNegativeY;

    


    [Header("Raw Image Display")]
    public RawImage computeRawImage;

    public RawImage directionRawImage1;
    public RawImage directionRawImage2;
    public RawImage directionRawImage3;
    public RawImage directionRawImage4;

    public RawImage directionRawImage5;
    public RawImage directionRawImage6;
    public RawImage directionRawImage7;
    public RawImage directionRawImage8;


    public RectTransform rectTransform;

    private Vector3[] corners = new Vector3[4];

    [Header("Factors")]
    public float speedOfSound = 343f;
    public float calculatedDeltaTime;
    public float updateDeltaTime;
    private float movingUpdateTimer;

    public float netTime;
    public int mouseInputX;
    public int mouseInputY;

    public float strengthFactor;
    public float strengthPower;

    public float elasicity;
    public float disperseRate;
    public float frequency;
    
    public bool randomize;

    public bool trackMouse;
    public bool drawCollider;
    public bool eraseCollider;
    public bool clearCompute;

    public Texture2D destinationTexture;

    [Header("UI")]
    public Slider updateSpeedSlider;
    public TextMeshProUGUI updateSpeedText;
    public bool calcFrameRate = false;
    public float[] frameRateBuffer = new float[10];

    public Slider frequencySlider;
    public TextMeshProUGUI frequencyText;

    public Button emitWaveButton;
    public Button drawColliderButton;
    public Button eraseColliderButton;

    public TMP_InputField resolutionInputField;
    public Button recalcGridButton;
    public TextMeshProUGUI gridSize;

    public Slider calculatedDeltaTimeSlider;
    public TextMeshProUGUI calculatedDeltaTimeText;


    public Slider elasticitySlider;
    public TextMeshProUGUI elasticityText;


    void Awake(){
        createNewRenderTexture();
        bufferMemory = bufferResolution/4;
        QualitySettings.asyncUploadBufferSize = 16;
    }

    void Start(){
        rectTransform.GetWorldCorners(corners);

        mouseInputX = -1;
        mouseInputY = -1;

        movingUpdateTimer = updateDeltaTime;
        airSimulationCompute.SetInt("firstPass", 1);
        netTime = 0;

        airSimulationCompute.SetInt("drawCollider",0);
    }

    public int floatLength(float someFloat){
        int digits = someFloat.ToString().Length-1;
        return digits;
    }

    public string formatFloat(float value, int digits){
        string str = "";
        float factor = Mathf.Pow(10f,digits);
        float roundedValue = (Mathf.Round(value * factor)) / factor;
        str += (""+roundedValue);
        
        int difference = digits - (floatLength(roundedValue)-1);
        for(int decimalPoint = 0; decimalPoint < difference; decimalPoint++)
        {
            str += "0";
        }
        
        return str;
    }

    public void updateFrameRate(){
        float frameRateAvg = Time.deltaTime;

        frameRateBuffer[0] = Time.deltaTime;
        for(int frameIndex = 1; frameIndex < frameRateBuffer.Length; frameIndex++){
            frameRateBuffer[frameIndex] = frameRateBuffer[frameIndex-1];
            frameRateAvg += frameRateBuffer[frameIndex];
        }
        frameRateAvg /= frameRateBuffer.Length;
        
        float hertz = Mathf.Pow(updateDeltaTime,-1f);
        updateSpeedText.text = hertz.ToString("#00.00")+" Hz";
    }

    public string frequencyToPitch(int frequency){
        string str = "";
        int nth = (int)(12f*Mathf.Log((((float)frequency)/440f),2f)+49f);
        int letterIndex = nth%12-4;
        if(letterIndex<0) letterIndex += 12;
        switch(letterIndex){
            case 0:
                str = "C";
                break;
            case 1:
                str = "C#/Db";
                break;
            case 2:
                str = "D";
                break;
            case 3:
                str = "D#/Eb";
                break;
            case 4:
                str = "E";
                break;
            case 5:
                str = "F";
                break;
            case 6:
                str = "F#/Gb";
                break;
            case 7:
                str = "G";
                break;
            case 8:
                str = "G#/Ab";
                break;
            case 9:
                str = "A";
                break;
            case 10:
                str = "A#/Bb";
                break;
            case 11:
                str = "B";
                break;
        }
        
        str += ""+Mathf.RoundToInt(((float)nth)/12f);
        
        //str += "\n"+nth;
        return str;
    }

    void UIInput(){
        //update Speed slider
        if(updateSpeedSlider.value != updateDeltaTime){
            if(updateSpeedSlider.value==updateSpeedSlider.minValue){
                calcFrameRate = true;
                movingUpdateTimer = -0.1f;
            }
            else{
                calcFrameRate = false;

                float hertz = Mathf.Pow(updateDeltaTime,-1f);

                updateDeltaTime = updateSpeedSlider.value;
                if(movingUpdateTimer>updateDeltaTime){
                    movingUpdateTimer = updateDeltaTime;
                }

                
                updateSpeedText.text = hertz.ToString("#00.00")+" Hz";
                
                
            }
        }
        if(calcFrameRate) {
            updateFrameRate();
        }

        //deltaTime slider
        if(calculatedDeltaTimeSlider.value != calculatedDeltaTime){
            calculatedDeltaTime = calculatedDeltaTimeSlider.value;
            calculatedDeltaTimeText.text = calculatedDeltaTime.ToString("#0.00000") + " s/px";
            calcSizeDomain();
        }

        //elasticity slider
        if(elasticitySlider.value != elasicity){
            elasicity = elasticitySlider.value;
            elasticityText.text = (elasicity*100f).ToString("#00") + "%";
            calcSizeDomain();
        }

        //frequency
        if(Mathf.Abs(((float)frequencySlider.value) - frequency) > 1.0){
            frequency = frequencySlider.value;
            int frequencyInteger = ((int)frequency);
            
            frequencyText.text = frequencyInteger + " Hz"+"\n"+frequencyToPitch(frequencyInteger);
        }
        
    }

    void createNewRenderTexture(){
        computeResult = new RenderTexture (bufferResolution, bufferResolution, 32, RenderTextureFormat.ARGB32);
        computeResult.enableRandomWrite = true;
        computeResult.name = "computeRender";
        computeResult.filterMode = FilterMode.Point;
        computeResult.Create();

        computeRawImage.texture = computeResult;

        /*
        directionResult = new RenderTexture (bufferResolution, bufferResolution, 32, RenderTextureFormat.ARGB32);
        directionResult.enableRandomWrite = true;
        directionResult.name = "directionResult";
        directionResult.filterMode = FilterMode.Point;
        directionResult.Create();

        directionRawImage.texture = directionResult;
        */

        directionResultPositiveX = new RenderTexture (bufferResolution, bufferResolution, 32, RenderTextureFormat.ARGB32);
        directionResultPositiveX.enableRandomWrite = true;
        directionResultPositiveX.name = "directionResultPositiveX";
        directionResultPositiveX.filterMode = FilterMode.Point;
        directionResultPositiveX.Create();

        directionRawImage1.texture = directionResultPositiveX;

        directionResultNegativeX = new RenderTexture (bufferResolution, bufferResolution, 32, RenderTextureFormat.ARGB32);
        directionResultNegativeX.enableRandomWrite = true;
        directionResultNegativeX.name = "directionResultNegativeX";
        directionResultNegativeX.filterMode = FilterMode.Point;
        directionResultNegativeX.Create();

        directionRawImage2.texture = directionResultNegativeX;

        directionResultPositiveY = new RenderTexture (bufferResolution, bufferResolution, 32, RenderTextureFormat.ARGB32);
        directionResultPositiveY.enableRandomWrite = true;
        directionResultPositiveY.name = "directionResultPositiveY";
        directionResultPositiveY.filterMode = FilterMode.Point;
        directionResultPositiveY.Create();

        directionRawImage3.texture = directionResultPositiveY;

        directionResultNegativeY = new RenderTexture (bufferResolution, bufferResolution, 32, RenderTextureFormat.ARGB32);
        directionResultNegativeY.enableRandomWrite = true;
        directionResultNegativeY.name = "directionResultNegativeY";
        directionResultNegativeY.filterMode = FilterMode.Point;
        directionResultNegativeY.Create();

        directionRawImage4.texture = directionResultNegativeY;


        //Diagonal Motion Vectors


        directionResultPositiveXPositiveY = new RenderTexture (bufferResolution, bufferResolution, 32, RenderTextureFormat.ARGB32);
        directionResultPositiveXPositiveY.enableRandomWrite = true;
        directionResultPositiveXPositiveY.name = "directionResultPositiveXPositiveY";
        directionResultPositiveXPositiveY.filterMode = FilterMode.Point;
        directionResultPositiveXPositiveY.Create();

        directionRawImage5.texture = directionResultPositiveXPositiveY;

        directionResultNegativeXPositiveY = new RenderTexture (bufferResolution, bufferResolution, 32, RenderTextureFormat.ARGB32);
        directionResultNegativeXPositiveY.enableRandomWrite = true;
        directionResultNegativeXPositiveY.name = "directionResultNegativeXPositiveY";
        directionResultNegativeXPositiveY.filterMode = FilterMode.Point;
        directionResultNegativeXPositiveY.Create();

        directionRawImage6.texture = directionResultNegativeXPositiveY;

        directionResultPositiveXNegativeY = new RenderTexture (bufferResolution, bufferResolution, 32, RenderTextureFormat.ARGB32);
        directionResultPositiveXNegativeY.enableRandomWrite = true;
        directionResultPositiveXNegativeY.name = "directionResultPositiveXNegativeY";
        directionResultPositiveXNegativeY.filterMode = FilterMode.Point;
        directionResultPositiveXNegativeY.Create();

        directionRawImage7.texture = directionResultPositiveXNegativeY;

        directionResultNegativeXNegativeY = new RenderTexture (bufferResolution, bufferResolution, 32, RenderTextureFormat.ARGB32);
        directionResultNegativeXNegativeY.enableRandomWrite = true;
        directionResultNegativeXNegativeY.name = "directionResultNegativeXNegativeY";
        directionResultNegativeXNegativeY.filterMode = FilterMode.Point;
        directionResultNegativeXNegativeY.Create();

        directionRawImage8.texture = directionResultNegativeXNegativeY;

        
    }

    void Update () {
        input();
        UIInput();

        netTime += Time.deltaTime;

        /*
        if(netTime >= 6.2832f){
            netTime -= 6.2832f;
        }
        */
        
        if(movingUpdateTimer <= 0f){
            runCompute();
            movingUpdateTimer = updateDeltaTime;
        }
        else{
            movingUpdateTimer -= Time.deltaTime;
        }

        if(Input.GetKeyDown(KeyCode.Alpha1)){
            onEmitWaveButtonPress();
            
        }

        if(Input.GetKeyDown(KeyCode.Alpha2)){
            onDrawColliderButtonPress();
        }

        if(Input.GetKeyDown(KeyCode.Alpha3)){
            onEraseColliderButtonPress();
        }

        if(Input.GetKeyDown(KeyCode.Alpha4)){
            saveResult();
        }
        

    }

    public void saveResult(){

        RenderTexture.active = computeResult;
        destinationTexture = new Texture2D(computeResult.width, computeResult.height, TextureFormat.RGB24, false);
        destinationTexture.filterMode = FilterMode.Point;
        destinationTexture.wrapMode = TextureWrapMode.Clamp;
        //destinationTexture.SetPixels(colourMap);
        destinationTexture.ReadPixels(new Rect(0, 0, computeResult.width, computeResult.height), 0, 0);
        RenderTexture.active = null;
        destinationTexture.Apply();

        

        byte[] bytes;
        bytes = destinationTexture.EncodeToPNG();
        string customFileName = "/"+bufferResolution+"_"+frequency+"_"+renderCount+".png";
        string path = Application.dataPath;

        //DownloadFile(bytes, bytes.Length, (customFileName+path));
        //Application.OpenURL(Application.streamingAssetsPath + customFileName);

        renderCount++;

        //string path = AssetDatabase.GetAssetPath(computeResult) + ".png";
        //System.IO.File.WriteAllBytes(path, bytes);
        //AssetDatabase.ImportAsset(path);

        File.WriteAllBytes( path + customFileName , bytes);

        Debug.Log("Saved to " + path);
    }

    void input(){
          
            Vector3 mousePos = Input.mousePosition;
            {
                float inputPercentageX = (mousePos.x - corners[0].x)/(corners[3].x - corners[0].x);
                float inputPercentageY = (mousePos.y - corners[0].y)/(corners[2].y - corners[0].y);

                if(inputPercentageX >= 0.0 && inputPercentageX <= 1.0 && inputPercentageY >= 0.0 && inputPercentageY <= 1.0){
                    mouseInputX = (int)(inputPercentageX * bufferResolution);
                    mouseInputY = (int)(inputPercentageY * bufferResolution);
                    if (Input.GetMouseButton(0)){  
                        trackMouse = true;
                    }
                    else{
                        trackMouse = false;
                    }
                
                }
        
        
            }
    }

    public void onEmitWaveButtonPress(){
        eraseCollider = false;
        drawCollider = false;

        emitWaveButton.interactable = false;
        drawColliderButton.interactable = true;
        eraseColliderButton.interactable = true;
    }

    public void onDrawColliderButtonPress(){
        eraseCollider = false;
        drawCollider = true;

        emitWaveButton.interactable = true;
        drawColliderButton.interactable = false;
        eraseColliderButton.interactable = true;
    }

    public void onEraseColliderButtonPress(){
        eraseCollider = true;
        drawCollider = false;

        emitWaveButton.interactable = true;
        drawColliderButton.interactable = true;
        eraseColliderButton.interactable = false;
    }

    public void calcSizeDomain(){
        //speed of sound * unit distance / delta time = meter distance

        float meterDistance = speedOfSound*((float)bufferResolution)*calculatedDeltaTime;
        string length = meterDistance.ToString("#0.00")+"m";
        gridSize.text = length+" x\n"+length;
    }

    public void onRecalcGridButtonPress(){
        bufferResolution = int.Parse(resolutionInputField.text);
        createNewRenderTexture();
        bufferMemory = bufferResolution/4;

        movingUpdateTimer = updateDeltaTime;
        airSimulationCompute.SetInt("firstPass", 1);
        clearCompute = true;
        netTime = 0;

        calcSizeDomain();
    }

    void runCompute() {

        int kernel = airSimulationCompute.FindKernel("FunctionKernel");
        airSimulationCompute.SetTexture(0, "computeResult", computeResult);

        //airSimulationCompute.SetTexture(0, "directionResult", directionResult);
        airSimulationCompute.SetTexture(0, "directionResultPositiveX", directionResultPositiveX);
        airSimulationCompute.SetTexture(0, "directionResultNegativeX", directionResultNegativeX);
        airSimulationCompute.SetTexture(0, "directionResultPositiveY", directionResultPositiveY);
        airSimulationCompute.SetTexture(0, "directionResultNegativeY", directionResultNegativeY);

        airSimulationCompute.SetTexture(0, "directionResultPositiveXPositiveY", directionResultPositiveXPositiveY);
        airSimulationCompute.SetTexture(0, "directionResultNegativeXPositiveY", directionResultNegativeXPositiveY);
        airSimulationCompute.SetTexture(0, "directionResultPositiveXNegativeY", directionResultPositiveXNegativeY);
        airSimulationCompute.SetTexture(0, "directionResultNegativeXNegativeY", directionResultNegativeXNegativeY);

        airSimulationCompute.SetInt("resolution", bufferResolution);
        airSimulationCompute.SetFloat("strengthFactor", strengthFactor);
        airSimulationCompute.SetFloat("strengthPower", strengthPower);

        airSimulationCompute.SetFloat("elasicity", elasicity);
        airSimulationCompute.SetFloat("disperseRate", disperseRate);


        airSimulationCompute.SetFloat("deltaTime",calculatedDeltaTime);
        airSimulationCompute.SetFloat("frequency",frequency);
        
        
        

        airSimulationCompute.SetInt("mouseInputX", mouseInputX);
        airSimulationCompute.SetInt("mouseInputY", mouseInputY);

        if(randomize){
            airSimulationCompute.SetInt("randomize", 1);
            randomize = false;
        }
        else{
            airSimulationCompute.SetInt("randomize", 0);
        }

        if(trackMouse){
            airSimulationCompute.SetInt("trackMouse", 1);
        }
        else{
            airSimulationCompute.SetInt("trackMouse", 0);
        }

        if(clearCompute){
            airSimulationCompute.SetInt("firstPass", 1);
            clearCompute = false;
        }
        else{
            airSimulationCompute.SetInt("firstPass", 0);
        }

        
            if(drawCollider) {
                airSimulationCompute.SetInt("drawCollider",1);
            }
            else{
                if(eraseCollider){
                    airSimulationCompute.SetInt("drawCollider",2);
                }
                else{
                    airSimulationCompute.SetInt("drawCollider",0);
                }
            }
        

        airSimulationCompute.SetFloat("netTime",netTime);

        

    
        airSimulationCompute.Dispatch(0, bufferMemory, bufferMemory, 1);
        //rawImage.texture = computeResult;

        

	}

}
