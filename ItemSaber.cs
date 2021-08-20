using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ThunderRoad;

namespace SaberMod
{
    public class ItemSaber : MonoBehaviour
    {
        private Item item;
        private AudioSource ignitionOnSound;
        private AudioSource ignitionOffSound;
        private AudioSource idleSound;
        private bool isSaberOn;
        private Transform saberBladesHolder;
        private List<GameObject> saberBlades = new List<GameObject>();
        private List<Vector3> bladeOffScales = new List<Vector3>();
        private List<Vector3> bladeOnScales = new List<Vector3>();
        private GameObject whoosh;
        private GameObject pierce;
        private Transform pierce2t;
        private GameObject pierce2;
        private float ignitionOnVol;
        private float ignitionOffVol;
        private float idleVol;
        private bool saberCycling;
        private bool saberDropped;
        private Coroutine co;
        private string bladeOrigColor;
        private List<GameObject> saberGlows = new List<GameObject>();
        private List<GameObject> saberTrails = new List<GameObject>();
        private int clickCounter;
        private bool colorCycling;
        private bool isDarksaber;
        private Material darksaberCutout;
        private bool recallAllowed;
        private bool recallTurnSaberOff;
        private float recallMaxDistance;
        private float recallStrength;
        private bool isRecalling;
        private float ignitionSpeed;
        private float ignitionDelay;

        public void Awake()
        {
            //hook up item and events
            item = GetComponent<Item>();
            item.OnGrabEvent += OnGrabEvent;
            item.OnUngrabEvent += OnUngrabEvent;
            item.OnTelekinesisGrabEvent += OnTelekinesisGrabEvent;
            item.OnTelekinesisReleaseEvent += OnTelekinesisReleaseEvent;
            item.OnSnapEvent += OnSnapEvent;
            item.OnHeldActionEvent += OnHeldActionEvent;

            //set variables
            isSaberOn = false;
            isRecalling = false;
            saberDropped = false;
            saberCycling = false;
            idleVol = 0.8f;
            ignitionOnVol = 1.0f;
            ignitionOffVol = 1.0f;

            //pull values from config
            recallAllowed = Configuration.RecallAllowed;
            recallStrength = Configuration.RecallStrength;
            recallTurnSaberOff = Configuration.RecallTurnSaberOff;
            recallMaxDistance = Configuration.RecallMaxDistance;
            ignitionSpeed = Configuration.IgnitionSpeed;
            ignitionDelay = Configuration.IgnitionDelay;
            //Debug.Log(recallStrength + " " + recallTurnSaberOff + " " + recallMaxDistance);


            saberBladesHolder = item.transform.Find("SaberBlade");
            whoosh = item.transform.Find("Whoosh").gameObject;
            pierce = item.transform.Find("Pierce").gameObject;
            pierce2t = item.transform.Find("Pierce2");
            if (pierce2t != null)
            {
                pierce2 = pierce2t.gameObject;
            }
            //set telegrab distance (default: 1)
            item.distantGrabSafeDistance = 3f;

            //set custom references
            ignitionOnSound = item.GetCustomReference("ignitionOnSound").GetComponent<AudioSource>();
            ignitionOffSound = item.GetCustomReference("ignitionOffSound").GetComponent<AudioSource>();
            idleSound = item.GetCustomReference("idleSound").GetComponent<AudioSource>();

            //set volumes on sounds
            ignitionOnSound.volume = ignitionOnVol;
            ignitionOffSound.volume = ignitionOffVol;
            idleSound.volume = idleVol;
            if (item.gameObject.name.Contains("Darksaber"))
            {
                isDarksaber = true;
                darksaberCutout = item.transform.Find("darksaber").GetComponent<MeshRenderer>().materials[2];
            }

            //for each blade, shrink and disable on awake
            for (int i = 0; i < saberBladesHolder.childCount; i++)
            {
                GameObject blade = saberBladesHolder.GetChild(i).gameObject;
                saberBlades.Add(blade);

                bladeOnScales.Add(new Vector3(0, blade.transform.localScale.y, 0));
                bladeOffScales.Add(new Vector3(0, -blade.transform.localScale.y, 0));
                blade.transform.localScale = new Vector3(blade.transform.localScale.x, 0, blade.transform.localScale.z);
                blade.GetComponent<Collider>().enabled = false;
                bladeOrigColor = blade.GetComponent<Renderer>().material.name;
                saberGlows.Add(blade.transform.Find("Glow").transform.gameObject);
                saberTrails.Add(blade.transform.Find("Trail").transform.gameObject);
                blade.SetActive(false);
            }

            //set whooshs beyond reach so they do not play.
            whoosh.GetComponent<WhooshPoint>().minVelocity = 9999.0f;
            whoosh.GetComponent<WhooshPoint>().maxVelocity = 9999.0f;

            //set up color changing variables, and light glows to match
            colorCycling = false;
            switch (bladeOrigColor.ToString())
            {
                case "BlueBlade (Instance)":
                    clickCounter = 1;
                    foreach(GameObject g in saberGlows)
                    {
                        g.GetComponent<Light>().color = Color.blue;
                    }
                    break;
                case "CyanBlade (Instance)":
                    clickCounter = 2;
                    foreach (GameObject g in saberGlows)
                    {
                        g.GetComponent<Light>().color = Color.green;
                    }
                    break;
                case "GreenBlade (Instance)":
                    clickCounter = 3;
                    foreach (GameObject g in saberGlows)
                    {
                        g.GetComponent<Light>().color = Color.green;
                    }
                    break;
                case "YellowBlade (Instance)":
                    clickCounter = 4;
                    foreach (GameObject g in saberGlows)
                    {
                        g.GetComponent<Light>().color = Color.yellow;
                    }
                    break;
                case "PurpleBlade (Instance)":
                    clickCounter = 5;
                    foreach (GameObject g in saberGlows)
                    {
                        g.GetComponent<Light>().color = Color.magenta;
                    }
                    break;
                case "RedBlade (Instance)":
                    clickCounter = 6;
                    foreach (GameObject g in saberGlows)
                    {
                        g.GetComponent<Light>().color = Color.red;
                    }
                    break;
                case "WhiteBlade (Instance)":
                    clickCounter = 0;
                    foreach (GameObject g in saberGlows)
                    {
                        g.GetComponent<Light>().color = Color.white;
                    }
                    break;
            }

            foreach (GameObject g in saberGlows)
            {
                g.SetActive(false);
            }

        }

        private void OnHeldActionEvent(RagdollHand ragdollHand, Handle handle, Interactable.Action action)
        {
            //if lightsaber is players hands
            if(action == Interactable.Action.AlternateUseStart)
            {
                if ((ragdollHand == Player.currentCreature.handLeft && item.IsHanded(PlayerControl.handLeft.side) && !PlayerControl.GetHand(PlayerControl.handLeft.side).castPressed)
                || (ragdollHand == Player.currentCreature.handRight && item.IsHanded(PlayerControl.handRight.side) && !PlayerControl.GetHand(PlayerControl.handRight.side).castPressed))
                {
                    //if saber is on
                    if (!isSaberOn)
                    {
                        if (!saberCycling)
                        {
                            //Debug.Log("turning saber " + item.gameObject.name + " on by button press");
                            co = StartCoroutine(ToggleSaber("on", "button"));
                        }
                    }
                    else
                    {
                        if (!saberCycling)
                        {
                            //Debug.Log("turning saber " + item.gameObject.name + " off by button press");
                            co = StartCoroutine(ToggleSaber("off", "button"));
                        }
                    }
                }

                else if(isSaberOn &&
                (ragdollHand == Player.currentCreature.handLeft && item.IsHanded(PlayerControl.handLeft.side) && PlayerControl.GetHand(PlayerControl.handLeft.side).castPressed)
                || (ragdollHand == Player.currentCreature.handRight && item.IsHanded(PlayerControl.handRight.side) && PlayerControl.GetHand(PlayerControl.handRight.side).castPressed))
                {
                    //if lightsaber is being held, and is turned on, cycle color
                    if (!colorCycling)
                    {
                        clickCounter++;
                        foreach (GameObject blade in saberBlades)
                        {
                            StartCoroutine(SwapColor(blade, clickCounter));
                        }
                    }
                }
            }
        }

        protected void OnGrabEvent(Handle handle, RagdollHand hand)
        {
            //if player grabbed saber in either hand
            if(hand.playerHand == Player.local.handLeft || hand.playerHand == Player.local.handRight)
            {  
                if (saberDropped && saberCycling)
                {
                    //reset dropped flag and cancel coroutine
                    saberDropped = false;
                    //Debug.Log("picked up saber, while still within drop window");
                    StopCoroutine(co);
                    //reset cycle bool
                    saberCycling = false;
                }
            }
            //if npc grabbed saber, turn on immediately
            else if (hand.playerHand != Player.local.handLeft && hand.playerHand != Player.local.handRight)
            {
                if (!saberCycling)
                {
                    //Debug.Log("npc grabbed saber " + item.gameObject.name + ", turn on");
                    co = StartCoroutine(ToggleSaber("on", "grab"));
                }
            }
        }

        protected void OnUngrabEvent(Handle handle, RagdollHand hand, bool throwing)
        {
            //if saber was dropped/let go of, not in a holder, and was dropped by player
            if (isSaberOn && !item.holder 
            && (hand.playerHand == Player.local.handLeft || hand.playerHand == Player.local.handRight))
            {
                if (saberCycling)
                {
                    StopCoroutine(co);
                    saberCycling = false;
                }
                //Debug.Log("player dropped saber " + item.gameObject.name + " was dropped, " + "handed:" + item.IsHanded() + " telgrabbed:" + item.isTelekinesisGrabbed + " isOn:" + isSaberOn + " mainHandler:" + item.mainHandler);
                co = StartCoroutine(ToggleSaber("off", "drop"));
            }
            //if saber was dropped from npc, and is currently on, turn off immediately
            else if (isSaberOn && !item.holder
                && (hand.playerHand != Player.local.handLeft && hand.playerHand != Player.local.handRight))
            {
                if (!saberCycling)
                {
                    //Debug.Log("saber " + item.gameObject.name + " was dropped, " + "handed:" + item.IsHanded() + " telgrabbed:" + item.isTelekinesisGrabbed + " isOn:" + isSaberOn + " mainHandler:" + item.mainHandler);
                    co = StartCoroutine(ToggleSaber("off", "npcdrop"));
                }
            }

            //if saber is on and not in a holder, and is penetrating a surface, turn off
            if (isSaberOn && !item.holder && item.isPenetrating)
            {
                if (!saberCycling)
                {
                    //Debug.Log(item.gameObject.name + " is currently penetrated, but not being held, turn off");
                    co = StartCoroutine(ToggleSaber("off", "drop"));
                }
            }
        }

        //when player grabs saber by telekinesis
        protected void OnTelekinesisGrabEvent(Handle handle, SpellTelekinesis teleGrabber)
        {
            //if saber was dropped and is currently turning off process
            if (saberDropped && saberCycling)
            {
                //Debug.Log("picked up saber, while still within drop window");
                //reset dropped flag and cancel process
                saberDropped = false;
                StopCoroutine(co);
                //reset cycle bool
                saberCycling = false;
            }
        }

        //when player drops saber after being held by telekinesis
        protected void OnTelekinesisReleaseEvent(Handle handle, SpellTelekinesis teleGrabber)
        {
            if (saberCycling)
            {
                StopCoroutine(co);
                saberCycling = false;
            }
            if (isSaberOn)
            {
                co = StartCoroutine(ToggleSaber("off", "drop"));
            }       
        }


        //when saber is placed in a holster
        protected void OnSnapEvent(Holder holder)
        {
            //if saber is in one of the holsters and blade is active, turn off
            if (isSaberOn)
            {
                //Debug.Log("saber " + item.gameObject.name + " was placed in holder, turning off");
                if (saberCycling)
                {
                    StopCoroutine(co);
                    saberCycling = false;
                }
                co = StartCoroutine(ToggleSaber("off", "holster"));
            }
        }

        public void Update()
        {
            //if no one is holding the saber in hand
            if (item.mainHandler == null)
            {
                //if saber is held by telekinesis and player presses grip and button to initiate spin, ignite saber
                if (!isSaberOn && item.isTelekinesisGrabbed &&
                ((PlayerControl.GetHand(PlayerControl.handLeft.side).alternateUsePressed && PlayerControl.GetHand(PlayerControl.handLeft.side).gripPressed) ||
                PlayerControl.GetHand(PlayerControl.handRight.side).alternateUsePressed && PlayerControl.GetHand(PlayerControl.handRight.side).gripPressed))
                {
                    if (!saberCycling)
                    {
                        //Debug.Log("turning saber " + item.gameObject.name + " on by telekinesis");
                        co = StartCoroutine(ToggleSaber("on", "tele"));
                    }
                }
            }
        }

        public void FixedUpdate()
        {

            //trail emit
            if (isSaberOn && item.lastHandler.creature == Player.currentCreature)
            {
                //get player velo
                float playerVelocity = Player.currentCreature.GetComponent<Rigidbody>().velocity.magnitude;
                float saberVelocity = item.GetComponent<Rigidbody>().velocity.magnitude;
                //subtract saber from player to get actual velo
                float trueVelocity = saberVelocity - playerVelocity; 
                if(trueVelocity > 2.0f)
                {
                    foreach(GameObject g in saberTrails)
                    {
                        if(!g.activeSelf) g.SetActive(true);          
                    }
                }
                else
                {
                    foreach (GameObject g in saberTrails)
                    {
                        if (g.activeSelf) g.SetActive(false);
                    }
                }
            } 
            else if (isSaberOn && item.IsHanded() && item.lastHandler.creature != Player.currentCreature)
            {
                //get npc velo
                RagdollHand npcHand = item.handlers[0];
                Creature npc = npcHand.creature;
                float playerVelocity = npc.GetComponent<Rigidbody>().velocity.magnitude;
                float saberVelocity = item.GetComponent<Rigidbody>().velocity.magnitude;
                //subtract saber from player to get actual velo
                float trueVelocity = saberVelocity - playerVelocity;
                if (trueVelocity > 2.0f)
                {
                    foreach (GameObject g in saberTrails)
                    {
                        if (!g.activeSelf) g.SetActive(true);
                    }
                }
                else
                {
                    foreach (GameObject g in saberTrails)
                    {
                        if (g.activeSelf) g.SetActive(false);
                    }
                }
            }

            //recall logic
            if (recallAllowed)
            {
                if (item.lastHandler != null && !item.IsHanded() && !item.isTelekinesisGrabbed)
                {
                    //if saber is not being held, but was held last by player and player presses grip and trigger, recall to player
                    if (item.lastHandler.creature == Player.currentCreature &&
                        ((PlayerControl.GetHand(PlayerControl.handRight.side).gripPressed && !Player.currentCreature.handRight.grabbedHandle)
                        || (PlayerControl.GetHand(PlayerControl.handLeft.side).gripPressed && !Player.currentCreature.handLeft.grabbedHandle)))
                    {
                        Handle handleL = item.mainHandleLeft;
                        Transform playerLeftHand = Player.currentCreature.handLeft.transform;
                        Transform playerRightHand = Player.currentCreature.handRight.transform;
                        Rigidbody rb = item.GetComponent<Rigidbody>();

                        //pull the saber back to the hand that gripped
                        if (PlayerControl.GetHand(PlayerControl.handLeft.side).gripPressed && !Player.currentCreature.handLeft.grabbedHandle)
                        {
                            float distance = Vector3.Distance(item.transform.position, playerLeftHand.position);
                            //Debug.Log("distance to left hand: " + Vector3.Distance(item.transform.position, playerLeftHand.position));
                            //distance must be greater than 5 to be recalled
                            if (distance > recallMaxDistance && !isRecalling)
                            {
                                isRecalling = true;
                            }
                            else if (distance >= 0.3f && isRecalling)
                            {
                                //turn off saber while recalling if on and config option is true
                                if (isSaberOn && recallTurnSaberOff)
                                {
                                    if (saberCycling)
                                    {
                                        StopCoroutine(co);
                                        saberCycling = false;
                                    }
                                    co = StartCoroutine(ToggleSaber("off", "recall"));
                                }
                                isRecalling = true;
                                Vector3 playerHandPos = playerLeftHand.position - item.transform.position;
                                rb.velocity = playerHandPos.normalized * recallStrength;
                            }
                            else if (distance < 0.3f && isRecalling)
                            {
                                //make player grab saber in hand
                                if (!Player.currentCreature.handLeft.grabbedHandle)
                                {
                                    Player.currentCreature.handLeft.Grab(handleL);
                                    isRecalling = false;
                                }

                            }

                        }
                        else if (PlayerControl.GetHand(PlayerControl.handRight.side).gripPressed && !Player.currentCreature.handRight.grabbedHandle)
                        {
                            float distance = Vector3.Distance(item.transform.position, playerRightHand.position);
                            //Debug.Log("distance to right hand: " + Vector3.Distance(item.transform.position, playerRightHand.position));
                            //if saber is not currently in hand, pull to hand
                            //distance must be greater than 5 to be recalled
                            if (distance > recallMaxDistance && !isRecalling)
                            {
                                isRecalling = true;
                            }
                            else if (distance >= 0.3f && isRecalling)
                            {
                                //turn off saber while recalling if on and config option is true
                                if (isSaberOn && recallTurnSaberOff)
                                {
                                    if (saberCycling)
                                    {
                                        StopCoroutine(co);
                                        saberCycling = false;
                                    }
                                    co = StartCoroutine(ToggleSaber("off", "recall"));
                                }
                                isRecalling = true;
                                Vector3 playerHandPos = playerRightHand.position - item.transform.position;
                                rb.velocity = playerHandPos.normalized * recallStrength;
                            }
                            else if (distance < 0.3f && isRecalling)
                            {
                                //make player grab saber in hand
                                if (!Player.currentCreature.handRight.grabbedHandle)
                                {
                                    Player.currentCreature.handRight.Grab(handleL);
                                    isRecalling = false;
                                }
                            }
                        }
                    }
                    else if (item.lastHandler.creature == Player.currentCreature)
                    {
                        //if saber is not being recalled but was(ie. dropped before recall finished) set isRecalling back to false and turn damagers back on
                        if (isRecalling)
                        {
                            isRecalling = false;
                        }
                    }
                }
            }              
        }

        private IEnumerator SwapColor(GameObject blade, int counter)
        {
            colorCycling = true;
            //Debug.Log("Changing color of saber..." + counter);
            switch (counter.ToString())
            {
                case "1":
                    //blue
                    Color blue = new Color(0, 40, 255, 130);
                    Color blueglow = new Color(0, 0, 191);
                    blade.GetComponent<Renderer>().material.SetColor("_Color", blue);
                    blade.GetComponent<Renderer>().material.SetColor("_EmissionColor", blueglow * 0.05f);
                    foreach (GameObject g in saberGlows)
                    {
                        g.GetComponent<Light>().color = Color.blue;
                    }
                    if (isDarksaber)
                    {
                        darksaberCutout.SetColor("_Color", blue);
                        darksaberCutout.SetColor("_EmissionColor", blueglow * 0.02f);
                    }
                    break;
                case "2":
                    //cyan
                    Color cyan = new Color(0, 255, 255, 130);
                    Color cyanglow = new Color(0, 191, 191);
                    blade.GetComponent<Renderer>().material.SetColor("_Color", cyan);
                    blade.GetComponent<Renderer>().material.SetColor("_EmissionColor", cyanglow * 0.04f);
                    foreach (GameObject g in saberGlows)
                    {
                        g.GetComponent<Light>().color = Color.cyan;
                    }
                    if (isDarksaber)
                    {
                        darksaberCutout.SetColor("_Color", cyan);
                        darksaberCutout.SetColor("_EmissionColor", cyanglow * 0.02f);
                    }
                    break;
                case "3":
                    //green
                    Color green = new Color(0, 255, 30, 130);
                    Color greenglow = new Color(0, 191, 0);
                    blade.GetComponent<Renderer>().material.SetColor("_Color", green);
                    blade.GetComponent<Renderer>().material.SetColor("_EmissionColor", greenglow * 0.03f);
                    foreach (GameObject g in saberGlows)
                    {
                        g.GetComponent<Light>().color = Color.green;
                    }
                    if (isDarksaber)
                    {
                        darksaberCutout.SetColor("_Color", green);
                        darksaberCutout.SetColor("_EmissionColor", greenglow * 0.02f);
                    }
                    break;
                case "4":
                    //yellow
                    Color yellow = new Color(255, 229, 0, 130);
                    Color yellowglow = new Color(191, 173, 0);
                    blade.GetComponent<Renderer>().material.SetColor("_Color", yellow);
                    blade.GetComponent<Renderer>().material.SetColor("_EmissionColor", yellowglow * 0.03f);
                    foreach (GameObject g in saberGlows)
                    {
                        g.GetComponent<Light>().color = Color.yellow;
                    }
                    if (isDarksaber)
                    {
                        darksaberCutout.SetColor("_Color", yellow);
                        darksaberCutout.SetColor("_EmissionColor", yellowglow * 0.02f);
                    }
                    break;
                case "5":
                    //purple
                    Color purple = new Color(152, 0, 255, 130);
                    Color purpleglow = new Color(60, 0, 191);
                    blade.GetComponent<Renderer>().material.SetColor("_Color", purple);
                    blade.GetComponent<Renderer>().material.SetColor("_EmissionColor", purpleglow * 0.05f);
                    foreach (GameObject g in saberGlows)
                    {
                        g.GetComponent<Light>().color = Color.magenta;
                    }
                    if (isDarksaber)
                    {
                        darksaberCutout.SetColor("_Color", purple);
                        darksaberCutout.SetColor("_EmissionColor", purpleglow * 0.02f);
                    }
                    break;
                case "6":
                    //red
                    Color red = new Color(255, 0, 0, 130);
                    Color redglow = new Color(191, 0, 0);
                    blade.GetComponent<Renderer>().material.SetColor("_Color", red);
                    blade.GetComponent<Renderer>().material.SetColor("_EmissionColor", redglow * 0.05f);
                    foreach (GameObject g in saberGlows)
                    {
                        g.GetComponent<Light>().color = Color.red;
                    }
                    if (isDarksaber)
                    {
                        darksaberCutout.SetColor("_Color", red);
                        darksaberCutout.SetColor("_EmissionColor", redglow * 0.02f);
                    }
                    break;
                case "7":
                    //white
                    Color white = new Color(255, 255, 255, 130);
                    Color whiteglow = new Color(191, 191, 191);
                    blade.GetComponent<Renderer>().material.SetColor("_Color", white);
                    blade.GetComponent<Renderer>().material.SetColor("_EmissionColor", whiteglow * 0.03f);
                    foreach (GameObject g in saberGlows)
                    {
                        g.GetComponent<Light>().color = Color.white;
                    }
                    if (isDarksaber)
                    {
                        darksaberCutout.SetColor("_Color", white);
                        darksaberCutout.SetColor("_EmissionColor", whiteglow * 0.02f);
                    }
                    //if saber is double bladed
                    if (saberBlades.Count > 1)
                    {
                        //and blade changing is second blade, reset counter to 0
                        if (saberBlades.IndexOf(blade) == 1)
                        {
                            clickCounter = 0;
                        }
                    }
                    else
                    {
                        clickCounter = 0;
                    }
                    break;
            }
            yield return new WaitForSeconds(0.25f);
            colorCycling = false;
        }

        private IEnumerator ToggleSaber(string state, string reason)
        {
            saberCycling = true;
            if (state == "on")
            {
                //play ignition sound of saber, then start timer, when elapsed play hum sound
                isSaberOn = true;
                //for each blade on saber, extend length
                for (int i = 0; i < saberBlades.Count; i++)
                {
                    GameObject blade = saberBlades[i];
                    StartCoroutine(ScaleOverTime(blade.transform, bladeOnScales[i], ignitionSpeed));
                }
                whoosh.GetComponent<WhooshPoint>().minVelocity = 2.0f;
                whoosh.GetComponent<WhooshPoint>().maxVelocity = 14.0f;
                foreach(GameObject g in saberGlows)
                {
                    g.SetActive(true);
                }
                ignitionOnSound.Play();
                idleSound.Play();
                yield return new WaitForSeconds(ignitionDelay);
                saberCycling = false;
                saberDropped = false;
            }
            else
            {
                //turn lightsaber off and stop idle and play ignitionOffSound
                if(reason == "drop")
                {
                    saberDropped = true;
                    //if dropped/let go, wait 3 seconds before turning off
                    yield return new WaitForSeconds(2.0f);
                }
                isSaberOn = false;
                idleSound.Stop();
                ignitionOffSound.Play();
                //set whooshes to unreachable velocity so the do not play when saber is turned off
                whoosh.GetComponent<WhooshPoint>().minVelocity = 9999.0f;
                whoosh.GetComponent<WhooshPoint>().maxVelocity = 9999.0f;
                //for each blade on saber, retract length
                for (int i = 0; i < saberBlades.Count; i++)
                {
                    GameObject blade = saberBlades[i];
                    StartCoroutine(ScaleOverTime(blade.transform, bladeOffScales[i], ignitionSpeed));
                }
                foreach (GameObject g in saberGlows)
                {
                    g.SetActive(false);
                }
                yield return new WaitForSeconds(ignitionDelay);
                saberCycling = false;
                saberDropped = false;
            }
        }

        private IEnumerator ScaleOverTime(Transform blade, Vector3 d, float t)
        {
            //on start make sure its active in case it's extending its visible
            if (!blade.gameObject.activeSelf)
            {
                blade.gameObject.SetActive(true);
            }
            //turn off collider of blade
            blade.GetComponent<Collider>().enabled = false;
            //turn off trail while saber in motion
            foreach(GameObject g in saberTrails)
            {
                g.SetActive(false);
            }

            float rate = 1 / t;
            float index = 0f;
            Vector3 startScale = blade.localScale;
            Vector3 endScale = startScale + d;
            //increase saber length over time provided (t)
            while (index < 1)
            {
                blade.localScale = Vector3.Lerp(startScale, endScale, index);
                index += rate * Time.deltaTime;
                yield return index;
            }
            //set blade length to end to make sure
            blade.localScale = endScale;

            //if blade scale is set to 0, turn off blade object
            if (blade.localScale == new Vector3(blade.localScale.x, 0, blade.localScale.z))
            {
                //if saber is penetrating surface, and has retracted, unpenetrate
                if (item.isPenetrating)
                {
                    pierce.GetComponent<Damager>().UnPenetrateAll();
                    if (pierce2 != null)
                    {
                        pierce2.GetComponent<Damager>().UnPenetrateAll();
                    }
                }
                blade.gameObject.SetActive(false);              
            }
            else
            {
                //when saber turns on
                blade.GetComponent<Collider>().enabled = true;
                foreach (GameObject g in saberTrails)
                {
                    g.SetActive(true);
                }
            }
            //reset colliders after length change
            item.ResetColliderCollision();
        }
    }
}
