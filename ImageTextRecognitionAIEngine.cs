using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System;

public class ImageTextRecognitionAIEngine : MonoBehaviour
{
    [SerializeField] List<string> knownImagePaths;
    [SerializeField] List<string> character;

    [SerializeField] string imagePath;
    [SerializeField] GameObject testObj;
    [SerializeField] GameObject imageObj;

    [Range(0, 1)]
    [SerializeField] float acceptanceAlphaValue = 0.5f;

    [Range(0, 1)]
    [SerializeField] float acceptanceRValue = 0.5f;

    [Range(0, 1)]
    [SerializeField] float acceptanceGValue = 0.5f;

    [Range(0, 1)]
    [SerializeField] float acceptanceBValue = 0.5f;

    [Range(0, 1)]
    [SerializeField] float numberBias = 0.5f;

    [Range(0, 1)]
    [SerializeField] float overallBias = 0.5f;

    [SerializeField] Vector2Int imageScale;

    [Range(0, 1)]
    [SerializeField] float compositionBias = 0.5f;

    [Range(0, 1)]
    [SerializeField] float matchBias = 0.5f;

    public string textFromImage;
    public bool analyse;
    public bool split;
    public bool splitAnimate;
    public bool detectObject;
    public bool detectObjectAnim;

    public string objectToDetect;

    public bool useScalling;

    public float waitTimer = 0.5f;

    public Color startCol;

    public Vector3 start;
    public Vector3 end;

    public Vector2 start2;
    public Vector2 end2;

    public Vector2 scale;

    public Vector2 objectToDetectPlaceholder;
    public Vector2 objectToDetectPlaceholderCenter;

    public Vector2 insidePlaceholer;

    public bool sameSize;

    public bool useWait;
    public bool useWaitSubPixel;

    public bool noSpawn;

    public bool matchOnlyColorsObjectHave;

    bool found;

    Color overlayColor = Color.green;

    private void Update()
    {
        if (analyse)
        {
            analyse = false;

            textFromImage = Analyse();
        }

        if (split)
        {
            split = false;

            GenerateSplitTexture(LoadPNG(imagePath));
        }
        
        if (splitAnimate)
        {
            splitAnimate = false;

            StartCoroutine(AnimateSplit(LoadPNG(imagePath)));
        }

        if (detectObject)
        {
            detectObject = false;

            ObjectDetection(LoadPNG(objectToDetect));
        }

        if (detectObjectAnim)
        {
            detectObjectAnim = false;
            StartCoroutine(DetectObject(LoadPNG(objectToDetect)));
        }
    }

    public void ChangeSpeed(string speed)
    {
        Time.timeScale = int.Parse(speed);
    }

    public string Analyse()
    {
        string text = "";

        List<float> thisImage = new List<float>();
        List<float> otherImage = new List<float>();

        foreach (var item in knownImagePaths)
        {
            otherImage = new List<float>();
            thisImage = new List<float>();
            
            Texture2D texture = useScalling ? ScaleTexture(LoadPNG(item), imageScale.x, imageScale.y) : LoadPNG(item);

            GameObject go = new GameObject();
            go.name = character[knownImagePaths.IndexOf(item)];

            float overall1 = 0;

            int sumValid = 0;
            for (int x = 0; x < texture.width; x++)
            {
                for (int y = 0; y < texture.height; y++)
                {
                    if (texture.GetPixel(x, y).a < acceptanceAlphaValue)
                    {
                        continue;
                    }

                    bool isValid = false;

                    if (texture.GetPixel(x, y).r <= acceptanceRValue && texture.GetPixel(x, y).g <= acceptanceGValue && texture.GetPixel(x, y).b <= acceptanceBValue && texture.GetPixel(x, y).a >= acceptanceAlphaValue)
                    {
                        isValid = true;
                    }

                    Debug.Log(isValid);

                    if (isValid)
                    {
                        Instantiate(testObj, new Vector3(x, y, 0), Quaternion.identity).transform.SetParent(go.transform);

                        sumValid++;
                        overall1++;
                    }
                }

                otherImage.Add(sumValid / texture.height);
            }

            overall1 = overall1 / (texture.width * texture.height);

            float overall2 = 0;

            texture = useScalling ? ScaleTexture(LoadPNG(imagePath), imageScale.x, imageScale.y) : LoadPNG(imagePath);

            go = new GameObject();

            sumValid = 0;
            for (int x = 0; x < texture.width; x++)
            {
                for (int y = 0; y < texture.height; y++)
                {
                    if (texture.GetPixel(x, y).a < acceptanceAlphaValue)
                    {
                        continue;
                    }

                    bool isValid = false;

                    if (texture.GetPixel(x, y).r <= acceptanceRValue && texture.GetPixel(x, y).g <= acceptanceGValue && texture.GetPixel(x, y).b <= acceptanceBValue && texture.GetPixel(x, y).a >= acceptanceAlphaValue)
                    {
                        isValid = true;
                    }

                    if (isValid)
                    {
                        Instantiate(testObj, new Vector3(x, y, 0), Quaternion.identity).transform.SetParent(go.transform);

                        sumValid++;
                        overall2++;
                    }
                }

                thisImage.Add(sumValid / texture.height);
            }

            bool broke = false;
            int overallMatch = 0;

            overall2 = overall2 / (texture.height * texture.width);

            if (LoadPNG(item).width == texture.width || true)
            {
                for (int i = 0; i < thisImage.Count; i++)
                {
                    if (otherImage[i] - numberBias > thisImage[i] || otherImage[i] + numberBias < thisImage[i])
                    {
                        //Out of range
                        broke = true;
                        continue;
                    }
                    
                    overallMatch++;
                }

                overallMatch = overallMatch / thisImage.Count;

                if (overall1 - overallBias <= overall2 && overall2 >= overall1 + overallBias)
                {
                    go.name = character[knownImagePaths.IndexOf(item)];
                    return character[knownImagePaths.IndexOf(item)];
                }

                if (broke)
                {
                    continue;
                }

                go.name = character[knownImagePaths.IndexOf(item)];
                return character[knownImagePaths.IndexOf(item)];
            }
        }

        return text;
    }

    public Texture2D GenerateSplitTexture(Texture2D texture2D)
    {
        Texture2D newTexture = new Texture2D(13, 13);

        GameObject go = new GameObject();

        for (int x = 0; x < texture2D.width; x++)
        {
            for (int y = 0; y < texture2D.height; y++)
            {
                if (texture2D.GetPixel(x, y).a < acceptanceAlphaValue)
                {
                    continue;
                }

                bool isValid = false;

                if (texture2D.GetPixel(x, y).r <= acceptanceRValue && texture2D.GetPixel(x, y).g <= acceptanceGValue && texture2D.GetPixel(x, y).b <= acceptanceBValue && texture2D.GetPixel(x, y).a >= acceptanceAlphaValue)
                {
                    isValid = true;
                }

                if (isValid)
                {
                    newTexture.SetPixel(x, y, Color.black);

                    Instantiate(testObj, new Vector3(x, y, 0), Quaternion.identity).transform.SetParent(go.transform);
                }
            }
        }

        return newTexture;
    }

    IEnumerator AnimateSplit(Texture2D texture2D)
    {
        GameObject go = new GameObject();

        for (int x = 0; x < texture2D.width; x++)
        {
            Instantiate(testObj, new Vector3(x, 0, 0), Quaternion.identity).transform.SetParent(go.transform);

            for (int y = 0; y < texture2D.height; y++)
            {
                //yield return new WaitForEndOfFrame();
                yield return new WaitForSeconds(waitTimer);

                if (texture2D.GetPixel(x, y).a < acceptanceAlphaValue)
                {
                    continue;
                }

                bool isValid = false;

                if (texture2D.GetPixel(x, y).r <= acceptanceRValue && texture2D.GetPixel(x, y).g <= acceptanceGValue && texture2D.GetPixel(x, y).b <= acceptanceBValue && texture2D.GetPixel(x, y).a >= acceptanceAlphaValue)
                {
                    isValid = true;
                }

                if (isValid)
                {
                    Instantiate(testObj, new Vector3(x, y, 0), Quaternion.identity).transform.SetParent(go.transform);
                }
            }
        }
    }

    public static Texture2D LoadPNG(string filePath)
    {
        Texture2D tex = null;
        byte[] fileData;

        if (File.Exists(filePath))
        {
            fileData = File.ReadAllBytes(filePath);
            tex = new Texture2D(2, 2);
            tex.LoadImage(fileData); //..t$$anonymous$$s will auto-resize the texture dimensions.
        }
        return tex;
    }

    private Texture2D ScaleTexture(Texture2D source, int targetWidth, int targetHeight)
    {
        Texture2D result = new Texture2D(targetWidth, targetHeight, source.format, true);
        Color[] rpixels = result.GetPixels(0);
        float incX = (1.0f / (float)targetWidth);
        float incY = (1.0f / (float)targetHeight);
        for (int px = 0; px < rpixels.Length; px++)
        {
            rpixels[px] = source.GetPixelBilinear(incX * ((float)px % targetWidth), incY * ((float)Mathf.Floor(px / targetWidth)));
        }
        result.SetPixels(rpixels, 0);
        result.Apply();
        return result;
    }

    public void ObjectDetection(Texture2D objectToDetect)
    {
        DateTime timeStart = DateTime.Now;
        found = false;

        Texture2D imageToAnalyse = LoadPNG(imagePath);

        GameObject px = Instantiate(imageObj);
        GameObject go = new GameObject();

        //Go through ref image
        Vector2 objectSizeRef = new(objectToDetect.width, objectToDetect.height);

        //object overlay
        objectToDetectPlaceholder = objectSizeRef;

        objectToDetectPlaceholderCenter = objectToDetectPlaceholder / 2;

        //image to analyse overlay
        end2 = new Vector2(imageToAnalyse.width, imageToAnalyse.height);

        List<Dictionary<Color, float>> objectRefColorComposition = new List<Dictionary<Color, float>>();
        List<Dictionary<Color, float>> imageObjectColorComposition = new List<Dictionary<Color, float>>();

        List<Color> colorsImageHave = new List<Color>();

        List<Color> cols = new List<Color>();

        //Getting composition of object
        for (int x = 0; x < objectToDetect.width; x++)
        {
            cols = new List<Color>();

            Dictionary<Color, float> temp = new Dictionary<Color, float>();

            for (int y = 0; y < objectToDetect.height; y++)
            {
                Color currentPixel = objectToDetect.GetPixel(x, y);

                if (temp.ContainsKey(currentPixel))
                {
                    temp[currentPixel] = (temp[currentPixel] + 1);
                }
                else
                {
                    temp.Add(currentPixel, 1);

                    cols.Add(currentPixel);
                }

                if (/*matchOnlyColorsObjectHave && */!colorsImageHave.Contains(currentPixel))
                {
                    colorsImageHave.Add(currentPixel);
                }
            }

            foreach (var item in cols)
            {
                temp[item] /= objectToDetect.height;
            }

            objectRefColorComposition.Add(temp);
        }

        bool isMatch = false;
        bool breakForce = false;

        int matches = 0;
        int total = 0;

        int tX = 0;
        int tY = 0;

        //Detection
        for (int x = 0; x < imageToAnalyse.width; x++)
        {
            breakForce = false;
            for (int y = 0; y < imageToAnalyse.height; y++)
            {
                breakForce = false;
                total = 0;
                matches = 0;

                objectToDetectPlaceholderCenter.y++;

                //Find composition of inside object
                insidePlaceholer = new Vector2();
                imageObjectColorComposition = new List<Dictionary<Color, float>>();

                bool getOut = false;

                for (int x1 = x; x1 < x + objectToDetectPlaceholder.x; x1++)
                {
                    cols = new List<Color>();

                    Dictionary<Color, float> temp = new Dictionary<Color, float>();

                    for (int y1 = y; y1 < y + objectToDetectPlaceholder.y; y1++)
                    {
                        total++;

                        Color currentPixel = imageToAnalyse.GetPixel(x1, y1);

                        Vector2Int pos = ImageToReferenceCoordinate(new Vector2Int(x, y), new Vector2Int(x1, y1));

                        Color col = objectToDetect.GetPixel(pos.x, pos.y);

                        if (currentPixel == col || (col.a == 0 && !colorsImageHave.Contains(currentPixel)))
                        {
                            matches++;
                            isMatch = true;
                        }
                        else
                        {
                            if (matchOnlyColorsObjectHave && !colorsImageHave.Contains(currentPixel) /*&& (col.a == 0 && colorsImageHave.Contains(currentPixel) || col.a != 0)*/)
                            {
                                breakForce = true;
                                break;
                            }
                        }
                    }

                    if (breakForce)
                    {
                        break;
                    }
                }

                //check if composition matches
                float ratio = ((matches + 0f) / (total + 0f));
                if (ratio >= matchBias && !breakForce)
                {
                    found = true;
                    int s = DateTime.Now.Subtract(timeStart).Seconds;
                    int ms = DateTime.Now.Subtract(timeStart).Milliseconds;

                    Debug.Log("Object Found! Time taken : " + s + ":" + ms + " ms");
                    Debug.Log("Efficiency : " + ((ms + 0f) / ((imageToAnalyse.width * imageToAnalyse.height) + 0f)) + " ms/px");
                    return;
                }
            }

            objectToDetectPlaceholderCenter.x++;
            objectToDetectPlaceholderCenter.y -= imageToAnalyse.height;
        }

        Debug.Log("Object Not Found!");
    }

    public void ObjectDetectionBk1(Texture2D objectToDetect)
    {
        DateTime timeStart = DateTime.Now;
        found = false;

        Texture2D imageToAnalyse = LoadPNG(imagePath);

        GameObject px = Instantiate(imageObj);
        GameObject go = new GameObject();

        //Go through ref image
        Vector2 objectSizeRef = new(objectToDetect.width, objectToDetect.height);

        //object overlay
        objectToDetectPlaceholder = objectSizeRef;

        objectToDetectPlaceholderCenter = objectToDetectPlaceholder / 2;

        //image to analyse overlay
        end2 = new Vector2(imageToAnalyse.width, imageToAnalyse.height);

        List<Dictionary<Color, float>> objectRefColorComposition = new List<Dictionary<Color, float>>();
        List<Dictionary<Color, float>> imageObjectColorComposition = new List<Dictionary<Color, float>>();

        List<Color> colorsImageHave = new List<Color>();

        List<Color> cols = new List<Color>();

        //Getting composition of object
        for (int x = 0; x < objectToDetect.width; x++)
        {
            cols = new List<Color>();

            Dictionary<Color, float> temp = new Dictionary<Color, float>();

            for (int y = 0; y < objectToDetect.height; y++)
            {
                Color currentPixel = objectToDetect.GetPixel(x, y);

                if (temp.ContainsKey(currentPixel))
                {
                    temp[currentPixel] = (temp[currentPixel] + 1);
                }
                else
                {
                    temp.Add(currentPixel, 1);

                    cols.Add(currentPixel);
                }

                if (matchOnlyColorsObjectHave && !colorsImageHave.Contains(currentPixel))
                {
                    colorsImageHave.Add(currentPixel);
                }
            }

            foreach (var item in cols)
            {
                temp[item] /= objectToDetect.height;
            }

            objectRefColorComposition.Add(temp);
        }

        //foreach (var item in objectRefColorComposition)
        //{
        //    foreach (var item2 in item)
        //    {
        //        Debug.Log(item2.Key + " : " + item2.Value);
        //    }

        //    Debug.Log("Column End");
        //}

        //Detection
        for (int x = 0; x < imageToAnalyse.width; x++)
        {
            for (int y = 0; y < imageToAnalyse.height; y++)
            {
                objectToDetectPlaceholderCenter.y++;

                //Find composition of inside object
                insidePlaceholer = new Vector2();
                imageObjectColorComposition = new List<Dictionary<Color, float>>();

                bool getOut = false;

                for (int x1 = x; x1 < x + objectToDetectPlaceholder.x; x1++)
                {
                    cols = new List<Color>();

                    Dictionary<Color, float> temp = new Dictionary<Color, float>();

                    for (int y1 = y; y1 < y + objectToDetectPlaceholder.y; y1++)
                    {
                        overlayColor = Color.green;
                        overlayColor.a = 0.2f;

                        px.transform.position = new Vector2(x1, y1);

                        insidePlaceholer = new Vector2(x1, y1);

                        Color currentPixel = imageToAnalyse.GetPixel(x1, y1);

                        for (int i = x1; i < x + objectToDetectPlaceholder.x; i += Mathf.CeilToInt((objectToDetectPlaceholder.x - 1)))
                        {
                            for (int j = y1; j < y + objectToDetectPlaceholder.y; j += Mathf.CeilToInt((objectToDetectPlaceholder.y - 1)))
                            {
                                currentPixel = imageToAnalyse.GetPixel(i, j);

                                Vector2Int objCoord = ImageToReferenceCoordinate(new Vector2Int(x, y), new Vector2Int(i, j));

                                Color objectPixel = objectToDetect.GetPixel(objCoord.x, objCoord.y);

                                objectPixel = objectPixel.a == 0 ? currentPixel : objectPixel;

                                if (matchOnlyColorsObjectHave && objectPixel != currentPixel/*!colorsImageHave.Contains(currentPixel)*/)
                                {
                                    x1 = Mathf.RoundToInt(x + objectToDetectPlaceholder.x) + 1;
                                    y1 = Mathf.RoundToInt(y + objectToDetectPlaceholder.y) + 1;

                                    overlayColor = Color.green;
                                    overlayColor.a = 0.2f;

                                    getOut = true;
                                    continue;
                                }
                            }
                        }

                        currentPixel = imageToAnalyse.GetPixel(x1, y1);

                        overlayColor = Color.green;
                        overlayColor.a = 1f;

                        if (temp.ContainsKey(currentPixel))
                        {
                            temp[currentPixel] = (temp[currentPixel] + 1);
                        }
                        else
                        {
                            temp.Add(currentPixel, 1);

                            cols.Add(currentPixel);
                        }

                        if (currentPixel == Color.white && !noSpawn)
                        {
                            GameObject img = Instantiate(imageObj, new Vector3(x1, y1, 0), Quaternion.identity);
                            img.transform.SetParent(go.transform);
                            img.GetComponent<SpriteRenderer>().color = Color.green;
                            img.transform.localScale = scale;
                        }
                    }

                    foreach (var item in cols)
                    {
                        temp[item] /= objectToDetectPlaceholder.y;
                    }

                    imageObjectColorComposition.Add(temp);
                }

                //check if composition matches
                bool isMatch = false;
                bool breakForce = false;

                int matches = 0;
                int total = 0;

                int tX = 0;
                int tY = 0;

                //Looping through each columns in image
                #region Old Comparison Code
                //foreach (var item3 in imageObjectColorComposition)
                //{
                //    //Looping through each columns in object
                //    foreach (var item4 in objectRefColorComposition)
                //    {
                //        //Looping through each color in image column composition
                //        foreach (var item in item3)
                //        {
                //            //Looping through each color in object column composition
                //            foreach (var item2 in item4)
                //            {
                //                total++;

                //                if (item.Key == item2.Key || item2.Key.a == 0)
                //                {
                //                    if ((item2.Value - compositionBias <= item.Value && item2.Value + compositionBias >= item.Value) /*|| item2.Key.a == 0*/)
                //                    {
                //                        isMatch = true;
                //                        matches++;
                //                    }
                //                    else
                //                    {
                //                        isMatch = false;

                //                        breakForce = true;

                //                        break;
                //                    }
                //                }
                //                else
                //                {
                //                    isMatch = false;

                //                    breakForce = true;

                //                    break;
                //                }
                //            }

                //            if (breakForce)
                //            {
                //                break;
                //            }
                //        }

                //        if (breakForce)
                //        {
                //            break;
                //        }
                //    }

                //    if (breakForce)
                //    {
                //        break;
                //    }
                //}
                #endregion

                if (imageObjectColorComposition.Count == objectRefColorComposition.Count)
                {
                    //Looping through each column
                    for (int i = 0; i < imageObjectColorComposition.Count; i++)
                    {
                        //Looping through each colors
                        foreach (var item in imageObjectColorComposition[i])
                        {
                            //Checking compositions
                            total++;

                            if (!objectRefColorComposition[i].ContainsKey(item.Key))
                            {
                                continue;
                            }

                            float comp = objectRefColorComposition[i][item.Key];

                            if (comp - compositionBias <= item.Value && item.Value <= comp + compositionBias)
                            {
                                isMatch = true;
                                matches++;
                            }
                        }
                    }
                }

                if (isMatch && (matches / total) >= matchBias && !breakForce)
                {
                    found = true;
                    int s = DateTime.Now.Subtract(timeStart).Seconds;
                    int ms = DateTime.Now.Subtract(timeStart).Milliseconds;

                    Debug.Log("Object Found! Time taken : " + s + ":" + ms + " ms");
                    Debug.Log("Efficiency : " + ((ms + 0f) / ((imageToAnalyse.width * imageToAnalyse.height) + 0f)) + " ms/px");
                    return;
                }
            }

            objectToDetectPlaceholderCenter.x++;
            objectToDetectPlaceholderCenter.y -= imageToAnalyse.height;
        }

        Debug.Log("Object Not Found!");
    }
    
    public void ObjectDetectionBk(Texture2D objectToDetect)
    {
        Texture2D imageToDetectIn = LoadPNG(imagePath);

        bool inObject = false;

        GameObject go = new GameObject();

        for (int x = 0; x < imageToDetectIn.width; x++)
        {
            for (int y = 0; y < imageToDetectIn.height; y++)
            {
                if (imageToDetectIn.GetPixel(x, y) == objectToDetect.GetPixel(sameSize ? x : 0, sameSize ? y : 0)/* || imageToDetectIn.GetPixel(x, y) == startCol*/)
                {
                    inObject = true;
                    Debug.Log("Found Object!");

                    GameObject img = Instantiate(imageObj, new Vector3(0, 0, 0), Quaternion.identity);
                    img.transform.SetParent(go.transform);
                    img.GetComponent<SpriteRenderer>().color = Color.green;

                    start = new Vector2(x, y);
                    start2 = new Vector2(0, 0);

                    Debug.Log(start);

                    end = new Vector2(x + objectToDetect.width, y + objectToDetect.height);
                    end2 = new Vector2(imageToDetectIn.width, imageToDetectIn.height);

                    img = Instantiate(imageObj, new Vector3(imageToDetectIn.width, imageToDetectIn.height, 0), Quaternion.identity);
                    img.transform.SetParent(go.transform);
                    img.GetComponent<SpriteRenderer>().color = Color.green;

                    return;
                }
            }
        }
    }

    IEnumerator DetectObject(Texture2D objectToDetect)
    {
        found = false;

        Texture2D imageToAnalyse = LoadPNG(imagePath);

        GameObject px = Instantiate(imageObj);
        GameObject go = new GameObject();

        //Go through ref image
        Vector2 objectSizeRef = new(objectToDetect.width, objectToDetect.height);

        //object overlay
        objectToDetectPlaceholder = objectSizeRef;

        objectToDetectPlaceholderCenter = objectToDetectPlaceholder / 2;

        //image to analyse overlay
        end2 = new Vector2(imageToAnalyse.width, imageToAnalyse.height);

        List<Dictionary<Color, float>> objectRefColorComposition = new List<Dictionary<Color, float>>();
        List<Dictionary<Color, float>> imageObjectColorComposition = new List<Dictionary<Color, float>>();

        List<Color> colorsImageHave = new List<Color>();

        List<Color> cols = new List<Color>();

        //Getting composition of object
        for (int x = 0; x < objectToDetect.width; x++)
        {
            cols = new List<Color>();

            Dictionary<Color, float> temp = new Dictionary<Color, float>();

            for (int y = 0; y < objectToDetect.height; y++)
            {
                Color currentPixel = objectToDetect.GetPixel(x, y);

                if (temp.ContainsKey(currentPixel))
                {
                    temp[currentPixel] = (temp[currentPixel] + 1);
                }
                else
                {
                    temp.Add(currentPixel, 1);

                    cols.Add(currentPixel);
                }

                if (/*matchOnlyColorsObjectHave && */!colorsImageHave.Contains(currentPixel))
                {
                    colorsImageHave.Add(currentPixel);
                }
            }

            foreach (var item in cols)
            {
                temp[item] /= objectToDetect.height;
            }

            objectRefColorComposition.Add(temp);
        }

        bool isMatch = false;
        bool breakForce = false;

        int matches = 0;
        int total = 0;

        int tX = 0;
        int tY = 0;

        //Detection
        for (int x = 0; x < imageToAnalyse.width; x++)
        {
            breakForce = false;
            for (int y = 0; y < imageToAnalyse.height; y++)
            {
                breakForce = false;
                total = 0;
                matches = 0;

                objectToDetectPlaceholderCenter.y++;

                //Find composition of inside object
                insidePlaceholer = new Vector2();
                imageObjectColorComposition = new List<Dictionary<Color, float>>();

                bool getOut = false;

                for (int x1 = x; x1 < x + objectToDetectPlaceholder.x; x1++)
                {
                    cols = new List<Color>();

                    Dictionary<Color, float> temp = new Dictionary<Color, float>();

                    for (int y1 = y; y1 < y + objectToDetectPlaceholder.y; y1++)
                    {
                        total++;

                        Color currentPixel = imageToAnalyse.GetPixel(x1, y1);

                        Vector2Int pos = ImageToReferenceCoordinate(new Vector2Int(x, y), new Vector2Int(x1, y1));

                        Color col = objectToDetect.GetPixel(pos.x, pos.y);

                        if (currentPixel == col || (col.a == 0 && !colorsImageHave.Contains(currentPixel)))
                        {
                            matches++;
                            isMatch = true;
                        }
                        else
                        {
                            if (matchOnlyColorsObjectHave && !colorsImageHave.Contains(currentPixel) /*&& (col.a == 0 && colorsImageHave.Contains(currentPixel) || col.a != 0)*/)
                            {
                                breakForce = true;
                                break;
                            }
                        }
                    }

                    if (breakForce)
                    {
                        break;
                    }

                    if (useWait)
                    {
                        yield return new WaitForSeconds(waitTimer);
                    }
                }

                //check if composition matches
                float ratio = ((matches + 0f) / (total + 0f));
                if (ratio >= matchBias && !breakForce)
                {
                    found = true;
                    
                    Debug.Log("Object Found!");

                    StopAllCoroutines();
                    yield break;
                }
            }

            objectToDetectPlaceholderCenter.x++;
            objectToDetectPlaceholderCenter.y -= imageToAnalyse.height;
        }
    }

    IEnumerator DetectObjectBk1(Texture2D objectToDetect)
    {
        found = false;

        Texture2D imageToAnalyse = LoadPNG(imagePath);

        GameObject px = Instantiate(imageObj);
        GameObject go = new GameObject();

        //Go through ref image
        Vector2 objectSizeRef = new(objectToDetect.width, objectToDetect.height);

        //object overlay
        objectToDetectPlaceholder = objectSizeRef;

        objectToDetectPlaceholderCenter = objectToDetectPlaceholder / 2;

        //image to analyse overlay
        end2 = new Vector2(imageToAnalyse.width, imageToAnalyse.height);

        List<Dictionary<Color, float>> objectRefColorComposition = new List<Dictionary<Color, float>>();
        List<Dictionary<Color, float>> imageObjectColorComposition = new List<Dictionary<Color, float>>();

        List<Color> colorsImageHave = new List<Color>();

        List<Color> cols = new List<Color>();

        //Getting composition of object
        for (int x = 0; x < objectToDetect.width; x++)
        {
            cols = new List<Color>();

            Dictionary<Color, float> temp = new Dictionary<Color, float>();

            for (int y = 0; y < objectToDetect.height; y++)
            {
                Color currentPixel = objectToDetect.GetPixel(x, y);

                if (temp.ContainsKey(currentPixel))
                {
                    temp[currentPixel] = (temp[currentPixel] + 1);
                }
                else
                {
                    temp.Add(currentPixel, 1);

                    cols.Add(currentPixel);
                }

                if (matchOnlyColorsObjectHave && !colorsImageHave.Contains(currentPixel))
                {
                    colorsImageHave.Add(currentPixel);
                }
            }

            foreach (var item in cols)
            {
                temp[item] /= objectToDetect.height;
            }

            objectRefColorComposition.Add(temp);
        }

        //foreach (var item in objectRefColorComposition)
        //{
        //    foreach (var item2 in item)
        //    {
        //        Debug.Log(item2.Key + " : " + item2.Value);
        //    }
        //    Debug.Log("[Column]");
        //}

        //Detection
        for (int x = 0; x < imageToAnalyse.width; x++)
        {
            for (int y = 0; y < imageToAnalyse.height; y++)
            {
                objectToDetectPlaceholderCenter.y++;

                //Find composition of inside object
                insidePlaceholer = new Vector2();
                imageObjectColorComposition = new List<Dictionary<Color, float>>();

                bool getOut = false;

                for (int x1 = x; x1 < x + objectToDetectPlaceholder.x; x1++)
                {
                    cols = new List<Color>();

                    Dictionary<Color, float> temp = new Dictionary<Color, float>();

                    for (int y1 = y; y1 < y + objectToDetectPlaceholder.y; y1++)
                    {
                        overlayColor = Color.green;
                        overlayColor.a = 0.2f;

                        px.transform.position = new Vector2(x1, y1);

                        insidePlaceholer = new Vector2(x1, y1);

                        Color currentPixel = imageToAnalyse.GetPixel(x1, y1);

                        //Checking Colors
                        for (int i = x; i < x + objectToDetectPlaceholder.x; i += Mathf.CeilToInt((objectToDetectPlaceholder.x - 1)))
                        {
                            for (int j = y; j < y + objectToDetectPlaceholder.y; j += Mathf.CeilToInt((objectToDetectPlaceholder.y - 1)))
                            {
                                currentPixel = imageToAnalyse.GetPixel(i, j);

                                Vector2Int objCoord = ImageToReferenceCoordinate(new Vector2Int(x, y), new Vector2Int(i, j));

                                Color objectPixel = objectToDetect.GetPixel(objCoord.x, objCoord.y);

                                objectPixel = objectPixel.a == 0 ? currentPixel : objectPixel;

                                if (matchOnlyColorsObjectHave && currentPixel != objectPixel /*!colorsImageHave.Contains(currentPixel)*/)
                                {
                                    x1 = Mathf.RoundToInt(x + objectToDetectPlaceholder.x) + 1;
                                    y1 = Mathf.RoundToInt(y + objectToDetectPlaceholder.y) + 1;

                                    overlayColor = Color.green;
                                    overlayColor.a = 0.2f;

                                    getOut = true;
                                    break;
                                }
                            }

                            if (getOut)
                            {
                                break;
                            }
                        }

                        if (getOut)
                        {
                            break;
                        }

                        currentPixel = imageToAnalyse.GetPixel(x1, y1);

                        overlayColor = Color.green;
                        overlayColor.a = 1f;

                        if (temp.ContainsKey(currentPixel))
                        {
                            temp[currentPixel] = (temp[currentPixel] + 1);
                        }
                        else
                        {
                            temp.Add(currentPixel, 1);

                            cols.Add(currentPixel);
                        }

                        if (currentPixel == Color.white && !noSpawn)
                        {
                            GameObject img = Instantiate(imageObj, new Vector3(x1, y1, 0), Quaternion.identity);
                            img.transform.SetParent(go.transform);
                            img.GetComponent<SpriteRenderer>().color = Color.green;
                            img.transform.localScale = scale;
                        }

                        if (useWaitSubPixel)
                        {
                            yield return new WaitForSeconds(waitTimer);
                        }
                    }

                    foreach (var item in cols)
                    {
                        temp[item] /= objectToDetectPlaceholder.y;
                    }

                    imageObjectColorComposition.Add(temp);
                }

                //check if composition matches
                bool isMatch = false;
                bool breakForce = false;

                int matches = 0;
                int total = 0;

                int tX = 0;
                int tY = 0;

                //Looping through each columns in image
                #region Old Comparison Code
                //foreach (var item3 in imageObjectColorComposition)
                //{
                //    //Looping through each columns in object
                //    foreach (var item4 in objectRefColorComposition)
                //    {
                //        //Looping through each color in image column composition
                //        foreach (var item in item3)
                //        {
                //            //Looping through each color in object column composition
                //            foreach (var item2 in item4)
                //            {
                //                total++;

                //                if (item.Key == item2.Key || item2.Key.a == 0)
                //                {
                //                    if ((item2.Value - compositionBias <= item.Value && item2.Value + compositionBias >= item.Value) /*|| item2.Key.a == 0*/)
                //                    {
                //                        isMatch = true;
                //                        matches++;
                //                    }
                //                    else
                //                    {
                //                        isMatch = false;

                //                        breakForce = true;

                //                        break;
                //                    }
                //                }
                //                else
                //                {
                //                    isMatch = false;

                //                    breakForce = true;

                //                    break;
                //                }
                //            }

                //            if (breakForce)
                //            {
                //                break;
                //            }
                //        }

                //        if (breakForce)
                //        {
                //            break;
                //        }
                //    }

                //    if (breakForce)
                //    {
                //        break;
                //    }
                //}
                #endregion

                if (imageObjectColorComposition.Count == objectRefColorComposition.Count)
                {
                    for (int i = 0; i < imageObjectColorComposition.Count; i++)
                    {
                        //foreach column
                        foreach (var item in imageObjectColorComposition[i])
                        {
                            try
                            {
                                float comp = objectRefColorComposition[i][item.Key];
                                total++;

                                if (comp - compositionBias <= item.Value && item.Value <= comp + compositionBias)
                                {
                                    isMatch = true;
                                    matches++;
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.Log("Error " + ex.Message);

                                //foreach (var deb in objectRefColorComposition[i])
                                //{
                                //    Debug.Log(item.Key + " : " + deb.Key);
                                //    Debug.Log(item.Key == deb.Key);
                                //}
                            }
                        }
                    }
                }

                if (isMatch && (matches / total) >= matchBias && !breakForce)
                {
                    found = true;
                    Debug.Log("Found!");
                    StopAllCoroutines();
                }

                if (useWait)
                {
                    yield return new WaitForSeconds(waitTimer);
                }
            }

            objectToDetectPlaceholderCenter.x++;
            objectToDetectPlaceholderCenter.y -= imageToAnalyse.height;

            if (!useWait)
            {
                yield return new WaitForSeconds(0);
            }
        }
    }

    IEnumerator DetectObjectBk(Texture2D objectToDetect)
    {
        GameObject img2 = Instantiate(imageObj, new Vector3(0, 0, 0), Quaternion.identity);
        img2.GetComponent<SpriteRenderer>().color = Color.green;
        img2.transform.localScale = scale;

        Texture2D imageToDetectIn = LoadPNG(imagePath);

        bool inObject = false;

        bool startO = false;
        bool endO = false;

        int xBias = -1;
        int yBias = -1;

        GameObject go = new GameObject();

        GameObject img = new GameObject();
        GameObject startPt = new GameObject();
        GameObject endPt = new GameObject();

        for (int x = 0; x < imageToDetectIn.width; x++)
        {
            if (startO)
            {
                xBias++;
                yBias = 0;
            }

            for (int y = 0; y < imageToDetectIn.height; y++)
            {
                if (startO)
                {
                    yBias++;
                }

                img2.transform.position = new Vector3(x, y, 0);

                if (imageToDetectIn.GetPixel(x, y) == objectToDetect.GetPixel(sameSize ? x : 0 + xBias, sameSize ? y : 0 + yBias))
                {
                    if (!noSpawn)
                    {
                        img = Instantiate(imageObj, new Vector3(x, y, 0), Quaternion.identity);
                        img.transform.SetParent(go.transform);
                        img.GetComponent<SpriteRenderer>().color = Color.green;
                        img.transform.localScale = scale;
                    }

                    if (!startO)
                    {
                        endO = false;
                        start = new Vector2(x, y);

                        if (!noSpawn)
                        {
                            img.GetComponent<SpriteRenderer>().color = Color.cyan;
                        }

                        startO = true;

                        if (!noSpawn)
                        {
                            startPt = img;
                        }
                        else
                        {
                            startPt.transform.position = new Vector3(x, y, 0);
                        }
                    }

                    if (!noSpawn)
                    {
                        endPt = img;
                    }
                    else
                    {
                        endPt.transform.position = new Vector3(x, y, 0);
                    }

                    inObject = true;

                    start2 = new Vector2(0, 0);

                    end2 = new Vector2(imageToDetectIn.width, imageToDetectIn.height);

                    if (useWait)
                    {
                        yield return new WaitForSeconds(waitTimer); 
                    }
                }
                else
                {
                    endO = true;
                }
            }

            if (!useWait)
            {
                yield return new WaitForSeconds(0);
            }
        }

        start = startPt.transform.position;
        end = endPt.transform.position;
    }

    IEnumerator DetectObjectBackup(Texture2D objectToDetect)
    {
        GameObject img2 = Instantiate(imageObj, new Vector3(0, 0, 0), Quaternion.identity);
        img2.GetComponent<SpriteRenderer>().color = Color.green;
        img2.transform.localScale = scale;

        Texture2D imageToDetectIn = LoadPNG(imagePath);

        bool inObject = false;

        bool startO = false;
        bool endO = false;

        int xBias = -1;
        int yBias = -1;

        GameObject go = new GameObject();

        for (int x = 0; x < imageToDetectIn.width; x++)
        {
            if (startO)
            {
                xBias++;
                yBias = 0;
            }

            for (int y = 0; y < imageToDetectIn.height; y++)
            {
                if (startO)
                {
                    yBias++;
                }

                img2.transform.position = new Vector3(x, y, 0);

                if (imageToDetectIn.GetPixel(x, y) /*!= Color.black*/== objectToDetect.GetPixel(sameSize ? x : 0 + xBias, sameSize ? y : 0 + yBias)/* || imageToDetectIn.GetPixel(x, y) == startCol*/)
                {
                    GameObject img = Instantiate(imageObj, new Vector3(x, y, 0), Quaternion.identity);
                    img.transform.SetParent(go.transform);
                    img.GetComponent<SpriteRenderer>().color = Color.green;
                    img.transform.localScale = scale;

                    if (!startO)
                    {
                        endO = false;
                        start = new Vector2(x, y);

                        Debug.Log(start);

                        img.GetComponent<SpriteRenderer>().color = Color.cyan;

                        startO = true;
                    }

                    inObject = true;
                    Debug.Log("Found Object!");

                    start2 = new Vector2(0, 0);

                    end = new Vector2(x, y);

                    Debug.Log(end);

                    end2 = new Vector2(imageToDetectIn.width, imageToDetectIn.height);

                    yield return new WaitForSeconds(waitTimer);
                }
                else
                {
                    endO = true;
                }

                //if (startO && endO)
                //{
                //    break;
                //}
            }

            if (startO && endO)
            {
                //break;
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube((start + end) / 2, end - start);

        Gizmos.DrawWireCube((start2 + end2) / 2, end2 - start2);

        Gizmos.color = overlayColor;
        if (found)
        {
            Gizmos.DrawWireCube(objectToDetectPlaceholderCenter, objectToDetectPlaceholder);
        }
        else
        {
            Gizmos.DrawCube(objectToDetectPlaceholderCenter, objectToDetectPlaceholder);
        }

        //Gizmos.DrawWireCube(insidePlaceholer / 2, insidePlaceholer / 10);
    }

    /// <summary>
    /// Converts the current point to the coordinates in the reference object
    /// </summary>
    /// <param name="startPt"></param>
    /// <param name="currentPt"></param>
    /// <returns></returns>
    Vector2Int ImageToReferenceCoordinate(Vector2Int startPt, Vector2Int currentPt)
    {
        return currentPt - startPt;
    }
}
