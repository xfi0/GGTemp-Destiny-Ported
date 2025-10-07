
using easyInputs;
using GGTemps.Classes;
using GGTemps.Notifications;
using MelonLoader;
using Photon.Pun;
using System;
using System.Collections;
using System.Linq;
using UnhollowerRuntimeLib;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;
using UnityEngine.UI;
using static GGTemps.Menu.Buttons;
using static GGTemps.Settings;
using Object = UnityEngine.Object;

namespace GGTemps.Menu 
{
    public class Main : MelonMod
    {
        // Constant

        public static float num = 2f;

        public static void MenuDeleteTime()
        {
            if (num == 2f)
                num = 5f; // Long
            else if (num == 5f)
                num = 0.01f; // Fast
            else
                num = 2f; // Default
        }
        public override void OnApplicationStart()
        {
            ClassInjector.RegisterTypeInIl2Cpp<TimedBehaviour>();
            ClassInjector.RegisterTypeInIl2Cpp<RigManager>();
            ClassInjector.RegisterTypeInIl2Cpp<ColorChanger>();
            ClassInjector.RegisterTypeInIl2Cpp<Classes.Button>();
            NotifiLib.Initialize();
            Patches.Menu.ApplyHarmonyPatches();
        }
        public override void OnUpdate()
        {
            // Initialize Menu
            try
            {
                NotifiLib.Update();
                bool toOpen = !rightHanded && EasyInputs.GetSecondaryButtonDown(EasyHand.LeftHand) || (rightHanded && EasyInputs.GetSecondaryButtonDown(EasyHand.RightHand));
                bool keyboardOpen = false;

                if (menu == null)
                {
                    if (toOpen || keyboardOpen)
                    {
                        CreateMenu();
                        RecenterMenu(rightHanded, keyboardOpen);
                        MelonCoroutines.Start(OpenAni());
                        if (reference == null)
                        {
                            CreateReference(rightHanded);
                        }
                    }
                }
                else
                {
                    if ((toOpen || keyboardOpen)) RecenterMenu(rightHanded, keyboardOpen);
                    else
                    {
                        MelonCoroutines.Start(CloseAni());
                    }
                }
            }
            catch (Exception exc)
            {
                UnityEngine.Debug.LogError(string.Format("{0} // Error initializing at {1}: {2}", PluginInfo.Name, exc.StackTrace, exc.Message));
            }

            // Constant
            try
            {
                // Pre-Execution
                if (fpsObject != null)
                {
                    fpsObject.text = "FPS: " + Mathf.Ceil(1f / Time.unscaledDeltaTime).ToString();
                }

                // Execute Enabled mods
                foreach (ButtonInfo[] buttonlist in buttons)
                {
                    foreach (ButtonInfo v in buttonlist)
                    {
                        if (v.enabled)
                        {
                            if (v.method != null)
                            {
                                try
                                {
                                    v.method.Invoke();
                                }
                                catch (Exception exc)
                                {
                                    UnityEngine.Debug.LogError(string.Format("{0} // Error with mod {1} at {2}: {3}", PluginInfo.Name, v.buttonText, exc.StackTrace, exc.Message));
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                UnityEngine.Debug.LogError(string.Format("{0} // Error with executing mods at {1}: {2}", PluginInfo.Name, exc.StackTrace, exc.Message));
            }
        }

        public static string openSfxUrl = "https://github.com/okcauq/menusounds/raw/refs/heads/main/ui-appear-101soundboards.mp3";
        public static string closeSfxUrl = "https://github.com/okcauq/menusounds/raw/refs/heads/main/ui-disappear-101soundboards.mp3";

        public static IEnumerator PlaySFX(string url)
        {
            UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG);
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
                Debug.LogError("Audio download error: " + www.error);
            
            else
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                GameObject go = new GameObject("VisionSFX");
                AudioSource source = go.AddComponent<AudioSource>();
                source.clip = clip;
                source.Play();
                UnityEngine.Object.Destroy(go, clip.length);
            }
        }

        private static IEnumerator AnimateTitle(Text text)
        {
            string targetText = PluginInfo.Name;
            string currentText = "";

            while (true)
            {
                for (int i = 0; i <= targetText.Length; i++)
                {
                    currentText = targetText.Substring(0, i);
                    text.text = currentText;
                    yield return new WaitForSeconds(0.25f);
                }
                yield return new WaitForSeconds(0.5f);
                text.text = "";
                yield return new WaitForSeconds(0.25f);
            }
        }



        public static IEnumerator UpdatePingText(Text pingText)
        {
            while (pingcounter)
            {
                pingText.text = "Ping: " + PhotonNetwork.GetPing().ToString();
                yield return new WaitForSeconds(0.1f);
            }
        }

        public static IEnumerator OpenAni()
        {
            if (menu == null) yield break;

            MelonCoroutines.Start(PlaySFX(openSfxUrl));
            float duration = 0.45f;
            float elapsed = 0f;
            Vector3 startScale = Vector3.zero;
            Vector3 targetScale = new Vector3(0.1f, 0.3f, 0.3825f);


            while (elapsed < duration)
            {
                if (menu == null) yield break;
                float t = elapsed / duration;

                float s = 1.70158f;
                t -= 1f;
                float bounce = (t * t * ((s + 1f) * t + s) + 1f);
                menu.transform.localScale = Vector3.LerpUnclamped(startScale, targetScale, bounce);
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (menu != null)
                menu.transform.localScale = targetScale;
        }





        public static IEnumerator CloseAni()
        {
            if (menu == null || Close) yield break;

            Close = true;
            MelonCoroutines.Start(PlaySFX(closeSfxUrl));
            float duration = 0.35f;
            float elapsed = 0f;
            Vector3 startScale = menu.transform.localScale;
            Vector3 targetScale = Vector3.zero;


            while (elapsed < duration)
            {
                if (menu == null) yield break;
                float t = elapsed / duration;

                float s = 1.70158f;
                float bounce = t * t * ((s + 1f) * t - s);
                menu.transform.localScale = Vector3.LerpUnclamped(startScale, targetScale, bounce);
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (menu != null)
                UnityEngine.Object.Destroy(menu);
            menu = null;

            if (reference != null)
                UnityEngine.Object.Destroy(reference);
            reference = null;

            Close = false;
        }
        public static bool Close = false;

        // Functions
        public static void CreateMenu()
        {
            menu = GameObject.CreatePrimitive(PrimitiveType.Cube);
            UnityEngine.Object.Destroy(menu.GetComponent<Rigidbody>());
            UnityEngine.Object.Destroy(menu.GetComponent<BoxCollider>());
            UnityEngine.Object.Destroy(menu.GetComponent<Renderer>());
            menu.transform.localScale = new Vector3(0.1f, 0.3f, 0.3825f);

            GameObject menuBackground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            UnityEngine.Object.Destroy(menuBackground.GetComponent<Rigidbody>());
            UnityEngine.Object.Destroy(menuBackground.GetComponent<BoxCollider>());
            menuBackground.transform.parent = menu.transform;
            menuBackground.transform.rotation = Quaternion.identity;
            menuBackground.transform.localScale = menuSize;
            menuBackground.transform.position = new Vector3(0.05f, 0f, 0f);
            menuBackground.GetComponent<Renderer>().material.color = new Color(101f / 255f, 158f / 255f, 105f / 255f, 1f);

            menuBackground.GetComponent<Renderer>().enabled = false;



            float bevel = 0.04f;
           

            Renderer ToRoundRenderer = menuBackground.GetComponent<Renderer>();


            GameObject BaseA11= GameObject.CreatePrimitive(PrimitiveType.Cube);
            BaseA11.GetComponent<Renderer>().enabled = true;
            UnityEngine.Object.Destroy(BaseA11.GetComponent<Collider>());
            BaseA11.transform.parent = menu.transform;
            BaseA11.transform.rotation = Quaternion.identity;
            BaseA11.transform.localPosition = menuBackground.transform.localPosition;
            BaseA11.transform.localScale = menuBackground.transform.localScale + new Vector3(0f, bevel * -2.55f, 0f);

            GameObject BaseB11 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            BaseB11.GetComponent<Renderer>().enabled = true;
            UnityEngine.Object.Destroy(BaseB11.GetComponent<Collider>());
            BaseB11.transform.parent = menu.transform;
            BaseB11.transform.rotation = Quaternion.identity;
            BaseB11.transform.localPosition = menuBackground.transform.localPosition;
            BaseB11.transform.localScale = menuBackground.transform.localScale + new Vector3(0f, 0f, -bevel * 2f);

            GameObject RoundCornerA11 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            RoundCornerA11.GetComponent<Renderer>().enabled = true;
            UnityEngine.Object.Destroy(RoundCornerA11.GetComponent<Collider>());
            RoundCornerA11.transform.parent = menu.transform;
            RoundCornerA11.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            RoundCornerA11.transform.localPosition = menuBackground.transform.localPosition + new Vector3(0f, (menuBackground.transform.localScale.y / 2f) - (bevel * 1.275f), (menuBackground.transform.localScale.z / 2f) - bevel);
            RoundCornerA11.transform.localScale = new Vector3(bevel * 2.55f, menuBackground.transform.localScale.x / 2f, bevel * 2f);

            GameObject RoundCornerB11 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            RoundCornerB11.GetComponent<Renderer>().enabled = true;
            UnityEngine.Object.Destroy(RoundCornerB11.GetComponent<Collider>());
            RoundCornerB11.transform.parent = menu.transform;
            RoundCornerB11.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            RoundCornerB11.transform.localPosition = menuBackground.transform.localPosition + new Vector3(0f, -(menuBackground.transform.localScale.y / 2f) + (bevel * 1.275f), (menuBackground.transform.localScale.z / 2f) - bevel);
            RoundCornerB11.transform.localScale = new Vector3(bevel * 2.55f, menuBackground.transform.localScale.x / 2f, bevel * 2f);

            GameObject RoundCornerC11 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            RoundCornerC11.GetComponent<Renderer>().enabled = true;
            UnityEngine.Object.Destroy(RoundCornerC11.GetComponent<Collider>());
            RoundCornerC11.transform.parent = menu.transform;
            RoundCornerC11.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            RoundCornerC11.transform.localPosition = menuBackground.transform.localPosition + new Vector3(0f, (menuBackground.transform.localScale.y / 2f) - (bevel * 1.275f), -(menuBackground.transform.localScale.z / 2f) + bevel);
            RoundCornerC11.transform.localScale = new Vector3(bevel * 2.55f, menuBackground.transform.localScale.x / 2f, bevel * 2f);

            GameObject RoundCornerD11 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            RoundCornerD11.GetComponent<Renderer>().enabled = true;
            UnityEngine.Object.Destroy(RoundCornerD11.GetComponent<Collider>());
            RoundCornerD11.transform.parent = menu.transform;
            RoundCornerD11.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            RoundCornerD11.transform.localPosition = menuBackground.transform.localPosition + new Vector3(0f, -(menuBackground.transform.localScale.y / 2f) + (bevel * 1.275f), -(menuBackground.transform.localScale.z / 2f) + bevel);
            RoundCornerD11.transform.localScale = new Vector3(bevel * 2.55f, menuBackground.transform.localScale.x / 2f, bevel * 2f);

            GameObject[] ToChange = new GameObject[]
            {
    BaseA11,
    BaseB11,
    RoundCornerA11,
    RoundCornerB11,
    RoundCornerC11,
    RoundCornerD11
            };


            if (outlineint)
            {
                menu2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                UnityEngine.Object.Destroy(menu.GetComponent<Rigidbody>());
                UnityEngine.Object.Destroy(menu.GetComponent<BoxCollider>());
                UnityEngine.Object.Destroy(menu.GetComponent<Renderer>());
                menu2.transform.localScale = new Vector3(0.1f, 0.3f, 0.3825f);

                GameObject menuBackground2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                UnityEngine.Object.Destroy(menuBackground2.GetComponent<Rigidbody>());
                UnityEngine.Object.Destroy(menuBackground2.GetComponent<BoxCollider>());
                menuBackground2.transform.parent = menu2.transform;
                menuBackground2.transform.rotation = Quaternion.identity;
                menuBackground2.transform.localScale = menuSize2;
                menuBackground2.transform.position = new Vector3(0.047f, 0f, 0f);
                

                menuBackground2.GetComponent<Renderer>().enabled = false;



                float bevel2 = 0.04f;
               

                Renderer ToRoundRenderer2 = menuBackground2.GetComponent<Renderer>();


                GameObject BaseA22 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                BaseA22.GetComponent<Renderer>().enabled = true;
                UnityEngine.Object.Destroy(BaseA22.GetComponent<Collider>());
                BaseA22.transform.parent = menu.transform;
                BaseA22.transform.rotation = Quaternion.identity;
                BaseA22.transform.localPosition = menuBackground2.transform.localPosition;
                BaseA22.transform.localScale = menuBackground2.transform.localScale + new Vector3(0f, bevel2 * -2.55f, 0f);

                GameObject BaseB22 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                BaseB22.GetComponent<Renderer>().enabled = true;
                UnityEngine.Object.Destroy(BaseB22.GetComponent<Collider>());
                BaseB22.transform.parent = menu.transform;
                BaseB22.transform.rotation = Quaternion.identity;
                BaseB22.transform.localPosition = menuBackground2.transform.localPosition;
                BaseB22.transform.localScale = menuBackground2.transform.localScale + new Vector3(0f, 0f, -bevel2 * 2f);

                GameObject RoundCornerA22 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                RoundCornerA22.GetComponent<Renderer>().enabled = true;
                UnityEngine.Object.Destroy(RoundCornerA22.GetComponent<Collider>());
                RoundCornerA22.transform.parent = menu.transform;
                RoundCornerA22.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
                RoundCornerA22.transform.localPosition = menuBackground2.transform.localPosition + new Vector3(0f, (menuBackground2.transform.localScale.y / 2f) - (bevel2 * 1.275f), (menuBackground2.transform.localScale.z / 2f) - bevel2);
                RoundCornerA22.transform.localScale = new Vector3(bevel2 * 2.55f, menuBackground2.transform.localScale.x / 2f, bevel2 * 2f);

                GameObject RoundCornerB22 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                RoundCornerB22.GetComponent<Renderer>().enabled = true;
                UnityEngine.Object.Destroy(RoundCornerB22.GetComponent<Collider>());
                RoundCornerB22.transform.parent = menu.transform;
                RoundCornerB22.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
                RoundCornerB22.transform.localPosition = menuBackground2.transform.localPosition + new Vector3(0f, -(menuBackground2.transform.localScale.y / 2f) + (bevel2 * 1.275f), (menuBackground2.transform.localScale.z / 2f) - bevel2);
                RoundCornerB22.transform.localScale = new Vector3(bevel2 * 2.55f, menuBackground2.transform.localScale.x / 2f, bevel2 * 2f);

                GameObject RoundCornerC22 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                RoundCornerC22.GetComponent<Renderer>().enabled = true;
                UnityEngine.Object.Destroy(RoundCornerC22.GetComponent<Collider>());
                RoundCornerC22.transform.parent = menu.transform;
                RoundCornerC22.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
                RoundCornerC22.transform.localPosition = menuBackground2.transform.localPosition + new Vector3(0f, (menuBackground2.transform.localScale.y / 2f) - (bevel2 * 1.275f), -(menuBackground2.transform.localScale.z / 2f) + bevel2);
                RoundCornerC22.transform.localScale = new Vector3(bevel2 * 2.55f, menuBackground2.transform.localScale.x / 2f, bevel2 * 2f);

                GameObject RoundCornerD22 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                RoundCornerD22.GetComponent<Renderer>().enabled = true;
                UnityEngine.Object.Destroy(RoundCornerD22.GetComponent<Collider>());
                RoundCornerD22.transform.parent = menu.transform;
                RoundCornerD22.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
                RoundCornerD22.transform.localPosition = menuBackground2.transform.localPosition + new Vector3(0f, -(menuBackground2.transform.localScale.y / 2f) + (bevel2 * 1.275f), -(menuBackground2.transform.localScale.z / 2f) + bevel2);
                RoundCornerD22.transform.localScale = new Vector3(bevel2 * 2.55f, menuBackground2.transform.localScale.x / 2f, bevel2 * 2f);

                GameObject[] ToChange2 = new GameObject[]
                {
                    BaseA22,
                    BaseB22,
                    RoundCornerA22,
                    RoundCornerB22,
                    RoundCornerC22,
                    RoundCornerD22
                };
                foreach (GameObject obj2 in ToChange2)
                {
                    obj2.GetComponent<Renderer>().material.color = new Color32(255, 100, 100, 255);
                }
            }

            foreach (GameObject obj in ToChange)
            {
                obj.GetComponent<Renderer>().material.color = new Color32(60, 15, 15, 255);
            }
        
            ColorChanger colorChanger = menuBackground.AddComponent<ColorChanger>();
            colorChanger.colorInfo = backgroundColor;
            colorChanger.Start();

            // Canvas
            canvasObject = new GameObject();
            canvasObject.transform.parent = menu.transform;
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            CanvasScaler canvasScaler = canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasScaler.dynamicPixelsPerUnit = 5000f;

            Text text = new GameObject
            {
                transform =
    {
        parent = canvasObject.transform
    }
            }.AddComponent<Text>();
            text.font = currentFont;
            text.text = "";
            text.fontSize = 1;
            text.color = textColors[0];
            text.supportRichText = true;
            text.fontStyle = FontStyle.Italic;
            text.alignment = TextAnchor.MiddleCenter;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 0;
            RectTransform component = text.GetComponent<RectTransform>();
            component.localPosition = Vector3.zero;
            component.sizeDelta = new Vector2(0.22f, 0.07f);
            component.position = new Vector3(0.06f, 0f, 0.17f);
            component.rotation = Quaternion.Euler(new Vector3(180f, 90f, 90f));

            // Start color transition
            MelonCoroutines.Start(new Main().AnimateColorTransition(text));
            if (animateTitle) MelonCoroutines.Start(AnimateTitle(text));
            else text.text = PluginInfo.Name;
        

// Color transition coroutine
        


            if (fpsCounter)
            {
                fpsObject = new GameObject
                {
                    transform =
                    {
                        parent = canvasObject.transform
                    }
                }.AddComponent<Text>();
                fpsObject.font = currentFont;
                fpsObject.text = "FPS: " + Mathf.Ceil(1f / Time.unscaledDeltaTime).ToString();
                fpsObject.color = textColors[0];
                fpsObject.fontSize = 1;
                fpsObject.supportRichText = true;
                fpsObject.fontStyle = FontStyle.Italic;
                fpsObject.alignment = TextAnchor.MiddleCenter;
                fpsObject.horizontalOverflow = UnityEngine.HorizontalWrapMode.Overflow;
                fpsObject.resizeTextForBestFit = true;
                fpsObject.resizeTextMinSize = 0;
                RectTransform component2 = fpsObject.GetComponent<RectTransform>();
                component2.localPosition = Vector3.zero;
                component2.sizeDelta = new Vector2(0.09f, 0.01f);
                component2.position = new Vector3(0.06f, 0.12f, -0.185f);
                component2.rotation = Quaternion.Euler(new Vector3(180f, 90f, 90f));
            }

            if (pingcounter)
            {
                Text text1 = new GameObject
                {
                    transform =
        {
            parent = canvasObject.transform
        }
                }.AddComponent<Text>();

                text1.font = currentFont;
                text1.fontSize = 1;
                text1.color = textColors[0];
                text1.supportRichText = true;
                text1.fontStyle = FontStyle.Italic;
                text1.alignment = TextAnchor.MiddleCenter;
                text1.resizeTextForBestFit = true;
                text1.resizeTextMinSize = 0;
                text1.text = "Ping: " + PhotonNetwork.GetPing().ToString();

                RectTransform component22 = text1.GetComponent<RectTransform>();
                component22.localPosition = Vector3.zero;
                component22.sizeDelta = new Vector2(0.09f, 0.01f);
                component22.position = new Vector3(0.06f, 0.08f, -0.185f);
                component22.rotation = Quaternion.Euler(new Vector3(180f, 90f, 90f));
                MelonCoroutines.Start(UpdatePingText(text1));
            }


            if (version)
            {
                Text text3 = new GameObject
                {
                    transform =
                    {
                        parent = canvasObject.transform
                    }
                }.AddComponent<Text>();
                text3.font = currentFont;
                text3.text = PluginInfo.Version;
                text3.fontSize = 1;
                text3.color = textColors[0];
                text3.supportRichText = true;
                text3.fontStyle = FontStyle.Italic;
                text3.alignment = TextAnchor.MiddleCenter;
                text3.resizeTextForBestFit = true;
                text3.resizeTextMinSize = 0;
                RectTransform component22 = text3.GetComponent<RectTransform>();
                component22.localPosition = Vector3.zero;
                component22.sizeDelta = new Vector2(0.09f, 0.01f);
                component22.position = new Vector3(0.06f, 0.05f, -0.185f);
                component22.rotation = Quaternion.Euler(new Vector3(180f, 90f, 90f));
            }

           

            GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            UnityEngine.Object.Destroy(gameObject.GetComponent<Rigidbody>());
            gameObject.GetComponent<BoxCollider>().isTrigger = true;
            gameObject.transform.parent = menu.transform;
            gameObject.transform.rotation = Quaternion.identity;
            gameObject.transform.localScale = new Vector3(0.09f, 0.43f, 0.1515f);
            gameObject.transform.localPosition = new Vector3(0.56f, 0.241f, -0.448f);

            gameObject.GetComponent<Renderer>().enabled = false;

            gameObject.AddComponent<Classes.Button>().relatedText = "PreviousPage";

            GameObject fillY = GameObject.CreatePrimitive(PrimitiveType.Cube);
            UnityEngine.Object.Destroy(fillY.GetComponent<Collider>());
            fillY.GetComponent<Renderer>().material.color = new Color32(140, 35, 35, 255);
            fillY.transform.parent = menu.transform;
            fillY.transform.rotation = Quaternion.identity;
            fillY.transform.localPosition = gameObject.transform.localPosition;
            fillY.transform.localScale = gameObject.transform.localScale + new Vector3(0f, -bevel * 2.55f, 0f);

            GameObject fillZ = GameObject.CreatePrimitive(PrimitiveType.Cube);
            UnityEngine.Object.Destroy(fillZ.GetComponent<Collider>());
            fillZ.GetComponent<Renderer>().material.color = new Color32(140, 35, 35, 255);
            fillZ.transform.parent = menu.transform;
            fillZ.transform.rotation = Quaternion.identity;
            fillZ.transform.localPosition = gameObject.transform.localPosition;
            fillZ.transform.localScale = gameObject.transform.localScale + new Vector3(0f, 0f, -bevel * 2f);


            Vector3 center = gameObject.transform.localPosition;
            Vector3 scale = gameObject.transform.localScale;

            void CreateCorner(Vector3 offset, string name)
            {
                GameObject corner = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                UnityEngine.Object.Destroy(corner.GetComponent<Collider>());
                corner.name = name;
                corner.GetComponent<Renderer>().material.color = new Color32(140, 35, 35, 255);
                corner.transform.parent = menu.transform;
                corner.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
                corner.transform.localPosition = center + offset;
                corner.transform.localScale = new Vector3(bevel * 2.55f, scale.x / 2f, bevel * 2f);
                corner.GetComponent<Renderer>().material.color = new Color32(140, 35, 35, 255);
            }

            CreateCorner(new Vector3(0f, (scale.y / 2f) - (bevel * 1.275f), (scale.z / 2f) - bevel), "CornerTopFront");
            CreateCorner(new Vector3(0f, -(scale.y / 2f) + (bevel * 1.275f), (scale.z / 2f) - bevel), "CornerBottomFront");
            CreateCorner(new Vector3(0f, (scale.y / 2f) - (bevel * 1.275f), -(scale.z / 2f) + bevel), "CornerTopBack");
            CreateCorner(new Vector3(0f, -(scale.y / 2f) + (bevel * 1.275f), -(scale.z / 2f) + bevel), "CornerBottomBack");
            fillY.GetComponent<Renderer>().material.color = new Color32(140, 35, 35, 255);
            fillZ.GetComponent<Renderer>().material.color = new Color32(140, 35, 35, 255);
            



            colorChanger = gameObject.AddComponent<ColorChanger>();
            colorChanger.colorInfo = buttonColors[0];
            colorChanger.Start();

            text = new GameObject
            {
                transform =
                        {
                            parent = canvasObject.transform
                        }
            }.AddComponent<Text>();
            text.font = currentFont;
            text.text = "<<<";
            text.fontSize = 1;
            text.color = textColors[0];
            text.alignment = TextAnchor.MiddleCenter;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 0;
            component = text.GetComponent<RectTransform>();
            component.localPosition = Vector3.zero;
            component.sizeDelta = new Vector2(0.2f, 0.03f);
            component.localPosition = new Vector3(0.064f, 0.070f, -0.168f);
            component.rotation = Quaternion.Euler(new Vector3(180f, 90f, 90f));

            GameObject nextPageOut = GameObject.CreatePrimitive(PrimitiveType.Cube);
            UnityEngine.Object.Destroy(nextPageOut.GetComponent<Rigidbody>());
            nextPageOut.GetComponent<BoxCollider>().isTrigger = true;
            nextPageOut.transform.parent = menu.transform;
            nextPageOut.transform.rotation = Quaternion.identity;
            nextPageOut.transform.localScale = new Vector3(0.09f, 0.45f, 0.16f);
            nextPageOut.transform.localPosition = new Vector3(0.54f, -0.228f, -0.448f);
            nextPageOut.GetComponent<Renderer>().enabled = false;

            GameObject prePageOUt = GameObject.CreatePrimitive(PrimitiveType.Cube);
           
            UnityEngine.Object.Destroy(prePageOUt.GetComponent<Rigidbody>());
            prePageOUt.GetComponent<BoxCollider>().isTrigger = true;
            prePageOUt.transform.parent = menu.transform;
            prePageOUt.transform.rotation = Quaternion.identity;
            prePageOUt.transform.localScale = new Vector3(0.09f, 0.45f, 0.16f);
            prePageOUt.transform.localPosition = new Vector3(0.54f, 0.241f, -0.448f);
            prePageOUt.GetComponent<Renderer>().enabled = false;




            GameObject nextPageButton = GameObject.CreatePrimitive(PrimitiveType.Cube);
           
            UnityEngine.Object.Destroy(nextPageButton.GetComponent<Rigidbody>());
            nextPageButton.GetComponent<BoxCollider>().isTrigger = true;
            nextPageButton.transform.parent = menu.transform;
            nextPageButton.transform.rotation = Quaternion.identity;
            nextPageButton.transform.localScale = new Vector3(0.09f, 0.43f, 0.1515f);
            nextPageButton.transform.localPosition = new Vector3(0.56f, -0.228f, -0.449f);
            nextPageButton.GetComponent<Renderer>().enabled = false;
            nextPageButton.AddComponent<Classes.Button>().relatedText = "NextPage";



            Color buttonFillColor = buttonColors[0].colors[0].color;
            float cornerRadius = 0.04f;

            GameObject nextPageFillY = GameObject.CreatePrimitive(PrimitiveType.Cube);
            UnityEngine.Object.Destroy(nextPageFillY.GetComponent<Collider>());
            nextPageFillY.GetComponent<Renderer>().material.color = buttonFillColor;
            nextPageFillY.transform.parent = menu.transform;
            nextPageFillY.transform.rotation = Quaternion.identity;
            nextPageFillY.transform.localPosition = nextPageButton.transform.localPosition;
            nextPageFillY.transform.localScale = nextPageButton.transform.localScale + new Vector3(0f, -cornerRadius * 2.55f, 0f);

            GameObject nextPageFillZ = GameObject.CreatePrimitive(PrimitiveType.Cube);
            UnityEngine.Object.Destroy(nextPageFillZ.GetComponent<Collider>());
            nextPageFillZ.GetComponent<Renderer>().material.color = buttonFillColor;
            nextPageFillZ.transform.parent = menu.transform;
            nextPageFillZ.transform.rotation = Quaternion.identity;
            nextPageFillZ.transform.localPosition = nextPageButton.transform.localPosition;
            nextPageFillZ.transform.localScale = nextPageButton.transform.localScale + new Vector3(0f, 0f, -cornerRadius * 2f);

            Vector3 buttonCenter = nextPageButton.transform.localPosition;
            Vector3 buttonScale = nextPageButton.transform.localScale;

            void CreateButtonCorner(Vector3 offset, string cornerName)
            {
                GameObject corner = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                UnityEngine.Object.Destroy(corner.GetComponent<Collider>());
                corner.name = cornerName;
                corner.GetComponent<Renderer>().material.color = buttonFillColor;
                corner.transform.parent = menu.transform;
                corner.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
                corner.transform.localPosition = buttonCenter + offset;
                corner.transform.localScale = new Vector3(cornerRadius * 2.55f, buttonScale.x / 2f, cornerRadius * 2f);
            }

            CreateButtonCorner(new Vector3(0f, (buttonScale.y / 2f) - (cornerRadius * 1.275f), (buttonScale.z / 2f) - cornerRadius), "TopFrontCorner");
            CreateButtonCorner(new Vector3(0f, -(buttonScale.y / 2f) + (cornerRadius * 1.275f), (buttonScale.z / 2f) - cornerRadius), "BottomFrontCorner");
            CreateButtonCorner(new Vector3(0f, (buttonScale.y / 2f) - (cornerRadius * 1.275f), -(buttonScale.z / 2f) + cornerRadius), "TopBackCorner");
            CreateButtonCorner(new Vector3(0f, -(buttonScale.y / 2f) + (cornerRadius * 1.275f), -(buttonScale.z / 2f) + cornerRadius), "BottomBackCorner");


            colorChanger = gameObject.AddComponent<ColorChanger>();
            colorChanger.colorInfo = buttonColors[0];
            colorChanger.Start();

            text = new GameObject
            {
                transform =
                        {
                            parent = canvasObject.transform
                        }
            }.AddComponent<Text>();
            text.font = currentFont;
            text.text = ">>>";
            text.fontSize = 1;
            text.color = textColors[0];
            text.alignment = TextAnchor.MiddleCenter;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 0;
            component = text.GetComponent<RectTransform>();
            component.localPosition = Vector3.zero;
            component.sizeDelta = new Vector2(0.2f, 0.03f);
            component.localPosition = new Vector3(0.064f, -0.066f, -0.168f);
            component.rotation = Quaternion.Euler(new Vector3(180f, 90f, 90f));

            if (outlineint)
            {
                GameObject BaseA1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                BaseA1.GetComponent<Renderer>().enabled = true;
                UnityEngine.Object.Destroy(BaseA1.GetComponent<Collider>());
                BaseA1.transform.parent = menu.transform;
                BaseA1.transform.rotation = Quaternion.identity;
                BaseA1.transform.localPosition = nextPageOut.transform.localPosition;
                BaseA1.transform.localScale = nextPageOut.transform.localScale + new Vector3(0f, bevel * -2.55f, 0f);

                GameObject BaseB1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                BaseB1.GetComponent<Renderer>().enabled = true;
                UnityEngine.Object.Destroy(BaseB1.GetComponent<Collider>());
                BaseB1.transform.parent = menu.transform;
                BaseB1.transform.rotation = Quaternion.identity;
                BaseB1.transform.localPosition = nextPageOut.transform.localPosition;
                BaseB1.transform.localScale = nextPageOut.transform.localScale + new Vector3(0f, 0f, -bevel * 2f);

                GameObject RoundCornerA1 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                RoundCornerA1.GetComponent<Renderer>().enabled = true;
                UnityEngine.Object.Destroy(RoundCornerA1.GetComponent<Collider>());
                RoundCornerA1.transform.parent = menu.transform;
                RoundCornerA1.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
                RoundCornerA1.transform.localPosition = nextPageOut.transform.localPosition + new Vector3(0f, (nextPageOut.transform.localScale.y / 2f) - (bevel * 1.275f), (nextPageOut.transform.localScale.z / 2f) - bevel);
                RoundCornerA1.transform.localScale = new Vector3(bevel * 2.55f, nextPageOut.transform.localScale.x / 2f, bevel * 2f);

                GameObject RoundCornerB1 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                RoundCornerB1.GetComponent<Renderer>().enabled = true;
                UnityEngine.Object.Destroy(RoundCornerB1.GetComponent<Collider>());
                RoundCornerB1.transform.parent = menu.transform;
                RoundCornerB1.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
                RoundCornerB1.transform.localPosition = nextPageOut.transform.localPosition + new Vector3(0f, -(nextPageOut.transform.localScale.y / 2f) + (bevel * 1.275f), (nextPageOut.transform.localScale.z / 2f) - bevel);
                RoundCornerB1.transform.localScale = new Vector3(bevel * 2.55f, nextPageOut.transform.localScale.x / 2f, bevel * 2f);

                GameObject RoundCornerC1 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                RoundCornerC1.GetComponent<Renderer>().enabled = true;
                UnityEngine.Object.Destroy(RoundCornerC1.GetComponent<Collider>());
                RoundCornerC1.transform.parent = menu.transform;
                RoundCornerC1.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
                RoundCornerC1.transform.localPosition = nextPageOut.transform.localPosition + new Vector3(0f, (nextPageOut.transform.localScale.y / 2f) - (bevel * 1.275f), -(nextPageOut.transform.localScale.z / 2f) + bevel);
                RoundCornerC1.transform.localScale = new Vector3(bevel * 2.55f, nextPageOut.transform.localScale.x / 2f, bevel * 2f);

                GameObject RoundCornerD1 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                RoundCornerD1.GetComponent<Renderer>().enabled = true;
                UnityEngine.Object.Destroy(RoundCornerD1.GetComponent<Collider>());
                RoundCornerD1.transform.parent = menu.transform;
                RoundCornerD1.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
                RoundCornerD1.transform.localPosition = nextPageOut.transform.localPosition + new Vector3(0f, -(nextPageOut.transform.localScale.y / 2f) + (bevel * 1.275f), -(nextPageOut.transform.localScale.z / 2f) + bevel);
                RoundCornerD1.transform.localScale = new Vector3(bevel * 2.55f, nextPageOut.transform.localScale.x / 2f, bevel * 2f);

                GameObject[] ToChange23 = new GameObject[]
                {
    BaseA1,
    BaseB1,
    RoundCornerA1,
    RoundCornerB1,
    RoundCornerC1,
    RoundCornerD1
                };

                foreach (GameObject obj2 in ToChange23)
                {
                    obj2.GetComponent<Renderer>().material.color = new Color32(255, 100, 100,255);
                }
            }

            if (outlineint)
            {
                GameObject BaseA12 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                BaseA12.GetComponent<Renderer>().enabled = true;
                UnityEngine.Object.Destroy(BaseA12.GetComponent<Collider>());
                BaseA12.transform.parent = menu.transform;
                BaseA12.transform.rotation = Quaternion.identity;
                BaseA12.transform.localPosition = prePageOUt.transform.localPosition;
                BaseA12.transform.localScale = prePageOUt.transform.localScale + new Vector3(0f, bevel * -2.55f, 0f);

                GameObject BaseB12 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                BaseB12.GetComponent<Renderer>().enabled = true;
                UnityEngine.Object.Destroy(BaseB12.GetComponent<Collider>());
                BaseB12.transform.parent = menu.transform;
                BaseB12.transform.rotation = Quaternion.identity;
                BaseB12.transform.localPosition = prePageOUt.transform.localPosition;
                BaseB12.transform.localScale = prePageOUt.transform.localScale + new Vector3(0f, 0f, -bevel * 2f);

                GameObject RoundCornerA12 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                RoundCornerA12.GetComponent<Renderer>().enabled = true;
                UnityEngine.Object.Destroy(RoundCornerA12.GetComponent<Collider>());
                RoundCornerA12.transform.parent = menu.transform;
                RoundCornerA12.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
                RoundCornerA12.transform.localPosition = prePageOUt.transform.localPosition + new Vector3(0f, (prePageOUt.transform.localScale.y / 2f) - (bevel * 1.275f), (prePageOUt.transform.localScale.z / 2f) - bevel);
                RoundCornerA12.transform.localScale = new Vector3(bevel * 2.55f, prePageOUt.transform.localScale.x / 2f, bevel * 2f);

                GameObject RoundCornerB12 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                RoundCornerB12.GetComponent<Renderer>().enabled = true;
                UnityEngine.Object.Destroy(RoundCornerB12.GetComponent<Collider>());
                RoundCornerB12.transform.parent = menu.transform;
                RoundCornerB12.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
                RoundCornerB12.transform.localPosition = prePageOUt.transform.localPosition + new Vector3(0f, -(prePageOUt.transform.localScale.y / 2f) + (bevel * 1.275f), (prePageOUt.transform.localScale.z / 2f) - bevel);
                RoundCornerB12.transform.localScale = new Vector3(bevel * 2.55f, prePageOUt.transform.localScale.x / 2f, bevel * 2f);

                GameObject RoundCornerC12 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                RoundCornerC12.GetComponent<Renderer>().enabled = true;
                UnityEngine.Object.Destroy(RoundCornerC12.GetComponent<Collider>());
                RoundCornerC12.transform.parent = menu.transform;
                RoundCornerC12.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
                RoundCornerC12.transform.localPosition = prePageOUt.transform.localPosition + new Vector3(0f, (prePageOUt.transform.localScale.y / 2f) - (bevel * 1.275f), -(prePageOUt.transform.localScale.z / 2f) + bevel);
                RoundCornerC12.transform.localScale = new Vector3(bevel * 2.55f, prePageOUt.transform.localScale.x / 2f, bevel * 2f);

                GameObject RoundCornerD12 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                RoundCornerD12.GetComponent<Renderer>().enabled = true;
                UnityEngine.Object.Destroy(RoundCornerD12.GetComponent<Collider>());
                RoundCornerD12.transform.parent = menu.transform;
                RoundCornerD12.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
                RoundCornerD12.transform.localPosition = prePageOUt.transform.localPosition + new Vector3(0f, -(prePageOUt.transform.localScale.y / 2f) + (bevel * 1.275f), -(prePageOUt.transform.localScale.z / 2f) + bevel);
                RoundCornerD12.transform.localScale = new Vector3(bevel * 2.55f, prePageOUt.transform.localScale.x / 2f, bevel * 2f);

                GameObject[] ToChange234 = new GameObject[]
                {
    BaseA12,
    BaseB12,
    RoundCornerA12,
    RoundCornerB12,
    RoundCornerC12,
    RoundCornerD12
                };

                foreach (GameObject obj24 in ToChange234)
                {
                    obj24.GetComponent<Renderer>().material.color = new Color32(255, 100, 100, 255);
                }
            }


            ButtonInfo[] activeButtons = buttons[buttonsType].Skip(pageNumber * buttonsPerPage).Take(buttonsPerPage).ToArray();
            for (int i = 0; i < activeButtons.Length; i++)
            {
                CreateButton(i * 0.1f, activeButtons[i]);
                CreateOtuline(i * 0.1f, activeButtons[i]);
            }
        }

        public static void CreateButton(float offset, ButtonInfo method)
        {
            GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
           
            UnityEngine.Object.Destroy(gameObject.GetComponent<Rigidbody>());
            gameObject.GetComponent<BoxCollider>().isTrigger = true;
            gameObject.transform.parent = menu.transform;
            gameObject.transform.rotation = Quaternion.identity;

            gameObject.transform.localScale = new Vector3(0.06f, 0.9f, 0.09f);
            gameObject.transform.localPosition = new Vector3(0.56f, 0f, 0.32f - offset);

            gameObject.GetComponent<Renderer>().enabled = false;

            float Bevel = 0.02f;
            GameObject BaseA = GameObject.CreatePrimitive(PrimitiveType.Cube);
            BaseA.GetComponent<Renderer>().enabled = true;
            UnityEngine.Object.Destroy(BaseA.GetComponent<Collider>());
            BaseA.transform.parent = menu.transform;
            BaseA.transform.rotation = Quaternion.identity;
            BaseA.transform.localPosition = gameObject.transform.localPosition;
            BaseA.transform.localScale = gameObject.transform.localScale + new Vector3(0f, Bevel * -2.55f, 0f);
            BaseA.GetComponent<Renderer>().material.color = ButtonColor;

            GameObject BaseB = GameObject.CreatePrimitive(PrimitiveType.Cube);
            BaseB.GetComponent<Renderer>().enabled = true;
            UnityEngine.Object.Destroy(BaseB.GetComponent<Collider>());
            BaseB.transform.parent = menu.transform;
            BaseB.transform.rotation = Quaternion.identity;
            BaseB.transform.localPosition = gameObject.transform.localPosition;
            BaseB.transform.localScale = gameObject.transform.localScale + new Vector3(0f, 0f, -Bevel * 2f);
            BaseB.GetComponent<Renderer>().material.color = ButtonColor;

            GameObject RoundCornerA = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            RoundCornerA.GetComponent<Renderer>().enabled = true;
            UnityEngine.Object.Destroy(RoundCornerA.GetComponent<Collider>());
            RoundCornerA.transform.parent = menu.transform;
            RoundCornerA.transform.rotation = Quaternion.identity * Quaternion.Euler(0f, 0f, 90f);
            RoundCornerA.transform.localPosition = gameObject.transform.localPosition + new Vector3(0f, (gameObject.transform.localScale.y / 2f) - (Bevel * 1.275f), (gameObject.transform.localScale.z / 2f) - Bevel);
            RoundCornerA.transform.localScale = new Vector3(Bevel * 2.55f, gameObject.transform.localScale.x / 2f, Bevel * 2f);
            RoundCornerA.GetComponent<Renderer>().material.color = ButtonColor;

            GameObject RoundCornerB = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            RoundCornerB.GetComponent<Renderer>().enabled = true;
            UnityEngine.Object.Destroy(RoundCornerB.GetComponent<Collider>());
            RoundCornerB.transform.parent = menu.transform;
            RoundCornerB.transform.rotation = Quaternion.identity * Quaternion.Euler(0f, 0f, 90f);
            RoundCornerB.transform.localPosition = gameObject.transform.localPosition + new Vector3(0f, -(gameObject.transform.localScale.y / 2f) + (Bevel * 1.275f), (gameObject.transform.localScale.z / 2f) - Bevel);
            RoundCornerB.transform.localScale = new Vector3(Bevel * 2.55f, gameObject.transform.localScale.x / 2f, Bevel * 2f);
            RoundCornerB.GetComponent<Renderer>().material.color = ButtonColor;

            GameObject RoundCornerC = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            RoundCornerC.GetComponent<Renderer>().enabled = true;
            UnityEngine.Object.Destroy(RoundCornerC.GetComponent<Collider>());
            RoundCornerC.transform.parent = menu.transform;
            RoundCornerC.transform.rotation = Quaternion.identity * Quaternion.Euler(0f, 0f, 90f);
            RoundCornerC.transform.localPosition = gameObject.transform.localPosition + new Vector3(0f, (gameObject.transform.localScale.y / 2f) - (Bevel * 1.275f), -(gameObject.transform.localScale.z / 2f) + Bevel);
            RoundCornerC.transform.localScale = new Vector3(Bevel * 2.55f, gameObject.transform.localScale.x / 2f, Bevel * 2f);
            RoundCornerC.GetComponent<Renderer>().material.color = ButtonColor;

            GameObject RoundCornerD = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            RoundCornerD.GetComponent<Renderer>().enabled = true;
            UnityEngine.Object.Destroy(RoundCornerD.GetComponent<Collider>());
            RoundCornerD.transform.parent = menu.transform;
            RoundCornerD.transform.rotation = Quaternion.identity * Quaternion.Euler(0f, 0f, 90f);
            RoundCornerD.transform.localPosition = gameObject.transform.localPosition + new Vector3(0f, -(gameObject.transform.localScale.y / 2f) + (Bevel * 1.275f), -(gameObject.transform.localScale.z / 2f) + Bevel);
            RoundCornerD.transform.localScale = new Vector3(Bevel * 2.55f, gameObject.transform.localScale.x / 2f, Bevel * 2f);
            RoundCornerD.GetComponent<Renderer>().material.color = ButtonColor;

            gameObject.AddComponent<Classes.Button>().relatedText = method.buttonText;
            gameObject.GetComponent<Renderer>().material.color = ButtonColor;

            GameObject[] ToChange = new GameObject[]
            {
    BaseA,
    BaseB,
    RoundCornerA,
    RoundCornerB,
    RoundCornerC,
    RoundCornerD
            };
            foreach (GameObject obj in ToChange)
            {
                obj.GetComponent<Renderer>().material.color = buttonColors[method.enabled ? 1 : 0].colors[0].color;
            }
            

            Text text = new GameObject
            {
                transform =
                {
                    parent = canvasObject.transform
                }
            }.AddComponent<Text>();
            text.font = currentFont;
            text.text = method.buttonText;
            if (method.overlapText != null)
            {
                text.text = method.overlapText;
            }
            text.supportRichText = true;
            text.fontSize = 1;
            if (method.enabled)
            {
                text.color = textColors[1];
            }
            else
            {
                text.color = textColors[0];
            }
            text.alignment = TextAnchor.MiddleCenter;
            text.fontStyle = FontStyle.Italic;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 0;
            RectTransform component = text.GetComponent<RectTransform>();
            component.localPosition = Vector3.zero;
            component.sizeDelta = new Vector2(.15f, .015f);
            component.localPosition = new Vector3(.064f, 0, .125f - offset / 2.6f);
            component.rotation = Quaternion.Euler(new Vector3(180f, 90f, 90f));

            

            GameObject disconnectbutton = GameObject.CreatePrimitive(PrimitiveType.Cube);
           
            UnityEngine.Object.Destroy(disconnectbutton.GetComponent<Rigidbody>());
            disconnectbutton.GetComponent<BoxCollider>().isTrigger = true;
            disconnectbutton.transform.parent = menu.transform;
            disconnectbutton.transform.rotation = Quaternion.identity;
            disconnectbutton.transform.localScale = new Vector3(0.09f, 0.13f, 0.1f);
            disconnectbutton.transform.localPosition = new Vector3(0.56f, -0.60f, 0.45f);
            disconnectbutton.GetComponent<Renderer>().material.color = buttonColors[0].colors[0].color;
            disconnectbutton.AddComponent<Classes.Button>().relatedText = "Settings";
            disconnectbutton.GetComponent<Renderer>().enabled = false;
            float bevel = 0.043f;

            GameObject ButtonBaseA = GameObject.CreatePrimitive(PrimitiveType.Cube);
            UnityEngine.Object.Destroy(ButtonBaseA.GetComponent<Collider>());
            
            ButtonBaseA.transform.parent = menu.transform;
            ButtonBaseA.transform.rotation = Quaternion.identity;
            ButtonBaseA.transform.localPosition = disconnectbutton.transform.localPosition;
            ButtonBaseA.transform.localScale = disconnectbutton.transform.localScale + new Vector3(0f, -bevel * 2.55f, 0f);

            GameObject ButtonBaseB = GameObject.CreatePrimitive(PrimitiveType.Cube);
            UnityEngine.Object.Destroy(ButtonBaseB.GetComponent<Collider>());
            
            ButtonBaseB.transform.parent = menu.transform;
            ButtonBaseB.transform.rotation = Quaternion.identity;
            ButtonBaseB.transform.localPosition = disconnectbutton.transform.localPosition;
            ButtonBaseB.transform.localScale = disconnectbutton.transform.localScale + new Vector3(0f, 0f, -bevel * 2f);

            GameObject ButtonCornerA = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            UnityEngine.Object.Destroy(ButtonCornerA.GetComponent<Collider>());
          
            ButtonCornerA.transform.parent = menu.transform;
            ButtonCornerA.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            ButtonCornerA.transform.localPosition = disconnectbutton.transform.localPosition + new Vector3(0f, (disconnectbutton.transform.localScale.y / 2f) - (bevel * 1.275f), (disconnectbutton.transform.localScale.z / 2f) - bevel);
            ButtonCornerA.transform.localScale = new Vector3(bevel * 2.55f, disconnectbutton.transform.localScale.x / 2f, bevel * 2f);


            GameObject ButtonCornerB = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            UnityEngine.Object.Destroy(ButtonCornerB.GetComponent<Collider>());
            
            ButtonCornerB.transform.parent = menu.transform;
            ButtonCornerB.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            ButtonCornerB.transform.localPosition = disconnectbutton.transform.localPosition + new Vector3(0f, -(disconnectbutton.transform.localScale.y / 2f) + (bevel * 1.275f), (disconnectbutton.transform.localScale.z / 2f) - bevel);
            ButtonCornerB.transform.localScale = new Vector3(bevel * 2.55f, disconnectbutton.transform.localScale.x / 2f, bevel * 2f);

            GameObject ButtonCornerC = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            UnityEngine.Object.Destroy(ButtonCornerC.GetComponent<Collider>());
            
            ButtonCornerC.transform.parent = menu.transform;
            ButtonCornerC.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            ButtonCornerC.transform.localPosition = disconnectbutton.transform.localPosition + new Vector3(0f, (disconnectbutton.transform.localScale.y / 2f) - (bevel * 1.275f), -(disconnectbutton.transform.localScale.z / 2f) + bevel);
            ButtonCornerC.transform.localScale = new Vector3(bevel * 2.55f, disconnectbutton.transform.localScale.x / 2f, bevel * 2f);

            GameObject ButtonCornerD = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            UnityEngine.Object.Destroy(ButtonCornerD.GetComponent<Collider>());
           
            ButtonCornerD.transform.parent = menu.transform;
            ButtonCornerD.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            ButtonCornerD.transform.localPosition = disconnectbutton.transform.localPosition + new Vector3(0f, -(disconnectbutton.transform.localScale.y / 2f) + (bevel * 1.275f), -(disconnectbutton.transform.localScale.z / 2f) + bevel);
            ButtonCornerD.transform.localScale = new Vector3(bevel * 2.55f, disconnectbutton.transform.localScale.x / 2f, bevel * 2f);


            GameObject[] disconnectButtonParts = new GameObject[]
            {
    ButtonBaseA,
    ButtonBaseB,
    ButtonCornerA,
    ButtonCornerB,
    ButtonCornerC,
    ButtonCornerD
            };



            foreach (GameObject obj in disconnectButtonParts)
            {
                obj.GetComponent<Renderer>().material.color = buttonColors[0].colors[0].color;
            }

            



            Text discontext = new GameObject
            {
                transform =
                            {
                                parent = canvasObject.transform
                            }
            }.AddComponent<Text>();
            discontext.text = "⚙︎";
            discontext.font = currentFont;
            discontext.fontSize = 1; 
            discontext.color = textColors[0]; 
            discontext.alignment = TextAnchor.MiddleCenter; 
            discontext.resizeTextForBestFit = true; 
            discontext.resizeTextMinSize = 0; 

            RectTransform rectt = discontext.GetComponent<RectTransform>();
            rectt.localPosition = Vector3.zero;
            rectt.sizeDelta = new Vector2(0.2f, 0.03f);
            rectt.localPosition = new Vector3(0.064f, -0.2f, 0.170f);
            rectt.rotation = Quaternion.Euler(new Vector3(180f, 90f, 90f));
        }
        


        public static void CreateOtuline(float offset, ButtonInfo method)
        {
            GameObject gameObject2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            
            UnityEngine.Object.Destroy(gameObject2.GetComponent<Rigidbody>());
            gameObject2.GetComponent<BoxCollider>().isTrigger = true;
            gameObject2.transform.parent = menu.transform;
            gameObject2.transform.rotation = Quaternion.identity;

            gameObject2.transform.localScale = new Vector3(0.06f, 0.91f, 0.095f);
            gameObject2.transform.localPosition = new Vector3(0.54f, 0f, 0.32f - offset);

            gameObject2.GetComponent<Renderer>().enabled = false;

            float Bevel = 0.02f;
            GameObject BaseA2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            BaseA2.GetComponent<Renderer>().enabled = true;
            UnityEngine.Object.Destroy(BaseA2.GetComponent<Collider>());
            BaseA2.transform.parent = menu.transform;
            BaseA2.transform.rotation = Quaternion.identity;
            BaseA2.transform.localPosition = gameObject2.transform.localPosition;
            BaseA2.transform.localScale = gameObject2.transform.localScale + new Vector3(0f, Bevel * -2.55f, 0f);
            BaseA2.GetComponent<Renderer>().material.color = ButtonColor;

            GameObject BaseB2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            BaseB2.GetComponent<Renderer>().enabled = true;
            UnityEngine.Object.Destroy(BaseB2.GetComponent<Collider>());
            BaseB2.transform.parent = menu.transform;
            BaseB2.transform.rotation = Quaternion.identity;
            BaseB2.transform.localPosition = gameObject2.transform.localPosition;
            BaseB2.transform.localScale = gameObject2.transform.localScale + new Vector3(0f, 0f, -Bevel * 2f);
            BaseB2.GetComponent<Renderer>().material.color = ButtonColor;

            GameObject RoundCornerA2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            RoundCornerA2.GetComponent<Renderer>().enabled = true;
            UnityEngine.Object.Destroy(RoundCornerA2.GetComponent<Collider>());
            RoundCornerA2.transform.parent = menu.transform;
            RoundCornerA2.transform.rotation = Quaternion.identity * Quaternion.Euler(0f, 0f, 90f);
            RoundCornerA2.transform.localPosition = gameObject2.transform.localPosition + new Vector3(0f, (gameObject2.transform.localScale.y / 2f) - (Bevel * 1.275f), (gameObject2.transform.localScale.z / 2f) - Bevel);
            RoundCornerA2.transform.localScale = new Vector3(Bevel * 2.55f, gameObject2.transform.localScale.x / 2f, Bevel * 2f);
            RoundCornerA2.GetComponent<Renderer>().material.color = ButtonColor;
            
            GameObject RoundCornerB2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            RoundCornerB2.GetComponent<Renderer>().enabled = true;
            UnityEngine.Object.Destroy(RoundCornerB2.GetComponent<Collider>());
            RoundCornerB2.transform.parent = menu.transform;
            RoundCornerB2.transform.rotation = Quaternion.identity * Quaternion.Euler(0f, 0f, 90f);
            RoundCornerB2.transform.localPosition = gameObject2.transform.localPosition + new Vector3(0f, -(gameObject2.transform.localScale.y / 2f) + (Bevel * 1.275f), (gameObject2.transform.localScale.z / 2f) - Bevel);
            RoundCornerB2.transform.localScale = new Vector3(Bevel * 2.55f, gameObject2.transform.localScale.x / 2f, Bevel * 2f);
            RoundCornerB2.GetComponent<Renderer>().material.color = ButtonColor;

            GameObject RoundCornerC2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            RoundCornerC2.GetComponent<Renderer>().enabled = true;
            UnityEngine.Object.Destroy(RoundCornerC2.GetComponent<Collider>());
            RoundCornerC2.transform.parent = menu.transform;
            RoundCornerC2.transform.rotation = Quaternion.identity * Quaternion.Euler(0f, 0f, 90f);
            RoundCornerC2.transform.localPosition = gameObject2.transform.localPosition + new Vector3(0f, (gameObject2.transform.localScale.y / 2f) - (Bevel * 1.275f), -(gameObject2.transform.localScale.z / 2f) + Bevel);
            RoundCornerC2.transform.localScale = new Vector3(Bevel * 2.55f, gameObject2.transform.localScale.x / 2f, Bevel * 2f);
            RoundCornerC2.GetComponent<Renderer>().material.color = ButtonColor;

            GameObject RoundCornerD2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            RoundCornerD2.GetComponent<Renderer>().enabled = true;
            UnityEngine.Object.Destroy(RoundCornerD2.GetComponent<Collider>());
            RoundCornerD2.transform.parent = menu.transform;
            RoundCornerD2.transform.rotation = Quaternion.identity * Quaternion.Euler(0f, 0f, 90f);
            RoundCornerD2.transform.localPosition = gameObject2.transform.localPosition + new Vector3(0f, -(gameObject2.transform.localScale.y / 2f) + (Bevel * 1.275f), -(gameObject2.transform.localScale.z / 2f) + Bevel);
            RoundCornerD2.transform.localScale = new Vector3(Bevel * 2.55f, gameObject2.transform.localScale.x / 2f, Bevel * 2f);
            RoundCornerD2.GetComponent<Renderer>().material.color = ButtonColor;


            

            GameObject[] ToChange3 = new GameObject[]
            {
                BaseA2,
                BaseB2,
                RoundCornerA2,
                RoundCornerB2,
                RoundCornerC2,
                RoundCornerD2
            };
            foreach (GameObject obj3 in ToChange3)
            {
                obj3.GetComponent<Renderer>().material.color = new Color32(255, 100, 100, 255);
            }
        }





        public static void RecreateMenu()
        {
            if (menu != null)
            {
                UnityEngine.Object.Destroy(menu);
                menu = null;

                CreateMenu();
                RecenterMenu(rightHanded, false);
            }
        }

        public static void RecenterMenu(bool isRightHanded, bool isKeyboardCondition)
        {
            if (!isKeyboardCondition)
            {
                if (!isRightHanded)
                {
                    menu.transform.position = GorillaTagger.Instance.leftHandTransform.position;
                    menu.transform.rotation = GorillaTagger.Instance.leftHandTransform.rotation;
                }
                else
                {
                    menu.transform.position = GorillaTagger.Instance.rightHandTransform.position;
                    Vector3 rotation = GorillaTagger.Instance.rightHandTransform.rotation.eulerAngles;
                    rotation += new Vector3(0f, 0f, 180f);
                    menu.transform.rotation = Quaternion.Euler(rotation);
                }
            }
            else
            {
                if (TPC != null)
                {
                    TPC.transform.position = new Vector3(-999f, -999f, -999f);
                    TPC.transform.rotation = Quaternion.identity;
                    menu.transform.parent = TPC.transform;
                    menu.transform.position = (TPC.transform.position + (Vector3.Scale(TPC.transform.forward, new Vector3(0.5f, 0.5f, 0.5f)))) + (Vector3.Scale(TPC.transform.up, new Vector3(-0.02f, -0.02f, -0.02f)));
                    Vector3 rot = TPC.transform.rotation.eulerAngles;
                    rot = new Vector3(rot.x - 90, rot.y + 90, rot.z);
                    menu.transform.rotation = Quaternion.Euler(rot);

                    if (reference != null)
                    {
                        if (Mouse.current.leftButton.isPressed)
                        {
                            Ray ray = TPC.ScreenPointToRay(Mouse.current.position.ReadValue());
                            RaycastHit hit;
                            bool worked = Physics.Raycast(ray, out hit, 100);
                            if (worked)
                            {
                                Classes.Button collide = hit.transform.gameObject.GetComponent<Classes.Button>();
                                collide?.OnTriggerEnter(buttonCollider);
                            }
                        }
                        else
                        {
                            reference.transform.position = new Vector3(999f, -999f, -999f);
                        }
                    }
                }
            }
        }

        public static void CreateReference(bool isRightHanded)
        {
            reference = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            if (isRightHanded)
            {
                reference.transform.parent = GorillaTagger.Instance.leftHandTransform;
            }
            else
            {
                reference.transform.parent = GorillaTagger.Instance.rightHandTransform;
            }
            reference.GetComponent<Renderer>().material.color = backgroundColor.colors[0].color;
            reference.transform.localPosition = new Vector3(0f, -0.1f, 0f);
            reference.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            buttonCollider = reference.GetComponent<SphereCollider>();

            ColorChanger colorChanger = reference.AddComponent<ColorChanger>();
            colorChanger.colorInfo = backgroundColor;
            colorChanger.Start();
        }

        public static void Toggle(string buttonText)
        {
            int lastPage = ((buttons[buttonsType].Length + buttonsPerPage - 1) / buttonsPerPage) - 1;
            if (buttonText == "PreviousPage")
            {
                pageNumber--;
                if (pageNumber < 0)
                {
                    pageNumber = lastPage;
                }
            }
            else
            {
                if (buttonText == "NextPage")
                {
                    pageNumber++;
                    if (pageNumber > lastPage)
                    {
                        pageNumber = 0;
                    }
                }
                else
                {
                    ButtonInfo target = GetIndex(buttonText);
                    if (target != null)
                    {
                        if (target.isTogglable)
                        {
                            target.enabled = !target.enabled;
                            if (target.enabled)
                            {
                                NotifiLib.SendNotification("<color=grey>[</color><color=green>ENABLE</color><color=grey>]</color> " + target.toolTip);
                                if (target.enableMethod != null)
                                {
                                    try { target.enableMethod.Invoke(); } catch { }
                                }
                            }
                            else
                            {
                                NotifiLib.SendNotification("<color=grey>[</color><color=red>DISABLE</color><color=grey>]</color> " + target.toolTip);
                                if (target.disableMethod != null)
                                {
                                    try { target.disableMethod.Invoke(); } catch { }
                                }
                            }
                        }
                        else
                        {
                            NotifiLib.SendNotification("<color=grey>[</color><color=green>ENABLE</color><color=grey>]</color> " + target.toolTip);
                            if (target.method != null)
                            {
                                try { target.method.Invoke(); } catch { }
                            }
                        }
                    }
                    else
                    {
                        UnityEngine.Debug.LogError(buttonText + " does not exist");
                    }
                }
            }
            RecreateMenu();
        }
        public IEnumerator AnimateColorTransition(Text text)
        {
            Color color1 = new Color(140f / 255f, 35f / 255f, 35f / 255f);
            Color color2 = new Color(220f / 255f, 50f / 255f, 50f / 255f);
            float duration = 2f; 

            while (true)
            {
                // Transition from color1 to color2
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / duration;
                    text.color = Color.Lerp(color1, color2, t);
                    yield return null;
                }

                // Transition from color2 to color1
                elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / duration;
                    text.color = Color.Lerp(color2, color1, t);
                    yield return null;
                }
            }
        }

        public static GradientColorKey[] GetSolidGradient(Color color)
        {
            return new GradientColorKey[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) };
        }

        public static ButtonInfo GetIndex(string buttonText)
        {
            foreach (ButtonInfo[] buttons in buttons)
            {
                foreach (ButtonInfo button in buttons)
                {
                    if (button.buttonText == buttonText)
                    {
                        return button;
                    }
                }
            }

            return null;
        }

        //public static Texture2D LoadTextureFromResource(string resourceName)
        //{
        //    Assembly executingAssembly = Assembly.GetExecutingAssembly();
        //    Texture2D texture2D;
        //    using (Stream manifestResourceStream = executingAssembly.GetManifestResourceStream("GGTempslate.Arrows." + resourceName))
        //    {
        //        if (manifestResourceStream == null)
        //        {
        //            Debug.LogError("Resource not found: " + resourceName);
        //            texture2D = null;
        //        }
        //        else
        //        {
        //            byte[] array;
        //            using (MemoryStream memoryStream = new MemoryStream())
        //            {
        //                manifestResourceStream.CopyTo(memoryStream);
        //                array = memoryStream.ToArray();
        //            }
        //            Texture2D texture2D2 = new Texture2D(2, 2);
        //            if (texture2D2.LoadImage(array))
        //            {
        //                Debug.Log("Loaded texture: " + resourceName);
        //                texture2D = texture2D2;
        //            }
        //            else
        //            {
        //                Debug.LogError("Failed to load image from resource stream.");
        //                texture2D = null;
        //            }
        //        }
        //    }
        //    return texture2D;
        //}
    

        // Variables
        // Important
        // Objects
        public static GameObject menu;
        public static GameObject menuBackground;
        public static GameObject reference;
        public static GameObject canvasObject;
        public static GameObject menu2;
        public static GameObject menuBackground2;

        public static SphereCollider buttonCollider;
        public static Camera TPC;
        public static Text fpsObject;

        // Data
        public static int pageNumber = 0;
        public static int buttonsType = 0;
        public static bool outlineint = true;
    }
}
