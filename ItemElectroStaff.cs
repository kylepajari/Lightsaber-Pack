using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ThunderRoad;

namespace SaberMod
{
    public class ItemElectroStaff : MonoBehaviour
    {
        private Item item;
        private AudioSource ignitionOnSound;
        private AudioSource ignitionOffSound;
        private AudioSource idleSound;
        private bool isStaffOn;
        private GameObject ElectricityEmmiter;
        private Transform Blades;
        private GameObject tip1;
        private GameObject tip2;
        private GameObject tip3;
        private GameObject tip4;
        private GameObject whoosh;
        private float ignitionOnVol;
        private float ignitionOffVol;
        private float idleVol;
        private bool StaffCycling;
        private bool StaffDropped;
        private Coroutine co;
        private string bladeOrigColor;
        private int clickCounter;
        private bool colorCycling;
        private bool recallAllowed;
        private bool recallTurnStaffOff;
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
            isStaffOn = false;
            isRecalling = false;
            StaffDropped = false;
            StaffCycling = false;
            idleVol = 0.8f;
            ignitionOnVol = 1.0f;
            ignitionOffVol = 1.0f;

            //pull values from config
            recallAllowed = Configuration.RecallAllowed;
            recallStrength = Configuration.RecallStrength;
            recallTurnStaffOff = Configuration.RecallTurnSaberOff;
            recallMaxDistance = Configuration.RecallMaxDistance;
            ignitionSpeed = Configuration.IgnitionSpeed;
            ignitionDelay = Configuration.IgnitionDelay;
            whoosh = item.transform.Find("Whoosh").gameObject;
            ElectricityEmmiter = item.transform.Find("Electricity").gameObject;
            ElectricityEmmiter.SetActive(false);
            Blades = item.transform.Find("Blades");
            tip1 = Blades.Find("electric1").gameObject;
            tip2 = Blades.Find("electric2").gameObject;
            tip3 = Blades.Find("mesh1 (2)").gameObject;
            tip4 = Blades.Find("mesh1 (3)").gameObject;
            tip1.SetActive(false);
            tip2.SetActive(false);
            tip3.SetActive(true);
            tip4.SetActive(true);

            //set custom references
            ignitionOnSound = item.GetCustomReference("ignitionOnSound").GetComponent<AudioSource>();
            ignitionOffSound = item.GetCustomReference("ignitionOffSound").GetComponent<AudioSource>();
            idleSound = item.GetCustomReference("idleSound").GetComponent<AudioSource>();

            //set volumes on sounds
            ignitionOnSound.volume = ignitionOnVol;
            ignitionOffSound.volume = ignitionOffVol;
            idleSound.volume = idleVol;

            //set whooshs beyond reach so they do not play.
            whoosh.GetComponent<WhooshPoint>().minVelocity = 9999.0f;
            whoosh.GetComponent<WhooshPoint>().maxVelocity = 9999.0f;

            //set up color changing variables, and light glows to match
            colorCycling = false;

        }

        private void OnHeldActionEvent(RagdollHand ragdollHand, Handle handle, Interactable.Action action)
        {
            //if lightStaff is players hands
            if(action == Interactable.Action.AlternateUseStart)
            {
                if ((ragdollHand == Player.currentCreature.handLeft && item.IsHanded(PlayerControl.handLeft.side) && !PlayerControl.GetHand(PlayerControl.handLeft.side).castPressed)
                || (ragdollHand == Player.currentCreature.handRight && item.IsHanded(PlayerControl.handRight.side) && !PlayerControl.GetHand(PlayerControl.handRight.side).castPressed))
                {
                    //if Staff is on
                    if (!isStaffOn)
                    {
                        if (!StaffCycling)
                        {
                            //Debug.Log("turning Staff " + item.gameObject.name + " on by button press");
                            co = StartCoroutine(ToggleStaff("on", "button"));
                        }
                    }
                    else
                    {
                        if (!StaffCycling)
                        {
                            //Debug.Log("turning Staff " + item.gameObject.name + " off by button press");
                            co = StartCoroutine(ToggleStaff("off", "button"));
                        }
                    }
                }

                else if(isStaffOn &&
                (ragdollHand == Player.currentCreature.handLeft && item.IsHanded(PlayerControl.handLeft.side) && PlayerControl.GetHand(PlayerControl.handLeft.side).castPressed)
                || (ragdollHand == Player.currentCreature.handRight && item.IsHanded(PlayerControl.handRight.side) && PlayerControl.GetHand(PlayerControl.handRight.side).castPressed))
                {
                    //if lightStaff is being held, and is turned on, cycle color
                    if (!colorCycling)
                    {
                        //clickCounter++;
                        //foreach (GameObject blade in StaffBlades)
                        //{
                        //    StartCoroutine(SwapColor(blade, clickCounter));
                        //}
                    }
                }
            }
        }

        protected void OnGrabEvent(Handle handle, RagdollHand hand)
        {
            //if player grabbed Staff in either hand
            if(hand.playerHand == Player.local.handLeft || hand.playerHand == Player.local.handRight)
            {  
                if (StaffDropped && StaffCycling)
                {
                    //reset dropped flag and cancel coroutine
                    StaffDropped = false;
                    //Debug.Log("picked up Staff, while still within drop window");
                    StopCoroutine(co);
                    //reset cycle bool
                    StaffCycling = false;
                }
            }
            //if npc grabbed Staff, turn on immediately
            else if (hand.playerHand != Player.local.handLeft || hand.playerHand != Player.local.handRight)
            {
                if (!StaffCycling)
                {
                    //Debug.Log("npc grabbed Staff " + item.gameObject.name + ", turn on");
                    co = StartCoroutine(ToggleStaff("on", "grab"));
                }
            }
        }

        protected void OnUngrabEvent(Handle handle, RagdollHand hand, bool throwing)
        {
            //if Staff was dropped/let go of, not in a holder, and was dropped by player
            if (isStaffOn && !item.holder 
            && (hand.playerHand == Player.local.handLeft || hand.playerHand == Player.local.handRight))
            {
                if (StaffCycling)
                {
                    StopCoroutine(co);
                    StaffCycling = false;
                }
                //Debug.Log("player dropped Staff " + item.gameObject.name + " was dropped, " + "handed:" + item.IsHanded() + " telgrabbed:" + item.isTelekinesisGrabbed + " isOn:" + isStaffOn + " mainHandler:" + item.mainHandler);
                co = StartCoroutine(ToggleStaff("off", "drop"));
            }
        }

        //when player grabs Staff by telekinesis
        protected void OnTelekinesisGrabEvent(Handle handle, SpellTelekinesis teleGrabber)
        {
            //if Staff was dropped and is currently turning off process
            if (StaffDropped && StaffCycling)
            {
                //Debug.Log("picked up Staff, while still within drop window");
                //reset dropped flag and cancel process
                StaffDropped = false;
                StopCoroutine(co);
                //reset cycle bool
                StaffCycling = false;
            }
        }

        //when player drops Staff after being held by telekinesis
        protected void OnTelekinesisReleaseEvent(Handle handle, SpellTelekinesis teleGrabber)
        {
            if (StaffCycling)
            {
                StopCoroutine(co);
                StaffCycling = false;
            }
            if (isStaffOn)
            {
                co = StartCoroutine(ToggleStaff("off", "drop"));
            }       
        }


        //when Staff is placed in a holster
        protected void OnSnapEvent(Holder holder)
        {
            //if Staff is in one of the holsters and blade is active, turn off
            if (isStaffOn)
            {
                //Debug.Log("Staff " + item.gameObject.name + " was placed in holder, turning off");
                if (StaffCycling)
                {
                    StopCoroutine(co);
                    StaffCycling = false;
                }
                co = StartCoroutine(ToggleStaff("off", "holster"));
            }
        }

        public void Update()
        {
            //if no one is holding the Staff in hand
            if (item.mainHandler == null)
            {
                //if Staff is held by telekinesis and player presses grip and button to initiate spin, ignite Staff
                if (!isStaffOn && item.isTelekinesisGrabbed &&
                ((PlayerControl.GetHand(PlayerControl.handLeft.side).alternateUsePressed && PlayerControl.GetHand(PlayerControl.handLeft.side).gripPressed) ||
                PlayerControl.GetHand(PlayerControl.handRight.side).alternateUsePressed && PlayerControl.GetHand(PlayerControl.handRight.side).gripPressed))
                {
                    if (!StaffCycling)
                    {
                        //Debug.Log("turning Staff " + item.gameObject.name + " on by telekinesis");
                        co = StartCoroutine(ToggleStaff("on", "tele"));
                    }
                }
            }
        }

        public void FixedUpdate()
        {
            //recall logic
            if (recallAllowed)
            {
                if (item.lastHandler != null && !item.IsHanded() && !item.isTelekinesisGrabbed)
                {
                    //if Staff is not being held, but was held last by player and player presses grip and trigger, recall to player
                    if (item.lastHandler.creature == Player.currentCreature &&
                        ((PlayerControl.GetHand(PlayerControl.handRight.side).gripPressed && !Player.currentCreature.handRight.grabbedHandle)
                        || (PlayerControl.GetHand(PlayerControl.handLeft.side).gripPressed && !Player.currentCreature.handLeft.grabbedHandle)))
                    {
                        Handle handleL = item.mainHandleLeft;
                        Transform playerLeftHand = Player.currentCreature.handLeft.transform;
                        Transform playerRightHand = Player.currentCreature.handRight.transform;
                        Rigidbody rb = item.GetComponent<Rigidbody>();

                        //pull the Staff back to the hand that gripped
                        if (PlayerControl.GetHand(PlayerControl.handLeft.side).gripPressed)
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
                                //turn off Staff while recalling if on and config option is true
                                if (isStaffOn && recallTurnStaffOff)
                                {
                                    if (StaffCycling)
                                    {
                                        StopCoroutine(co);
                                        StaffCycling = false;
                                    }
                                    co = StartCoroutine(ToggleStaff("off", "recall"));
                                }
                                isRecalling = true;
                                Vector3 playerHandPos = playerLeftHand.position - item.transform.position;
                                rb.velocity = playerHandPos.normalized * recallStrength;
                            }
                            else if (distance < 0.3f && isRecalling)
                            {
                                //make player grab Staff in hand
                                if (!Player.currentCreature.handLeft.grabbedHandle)
                                {
                                    Player.currentCreature.handLeft.Grab(handleL);
                                    isRecalling = false;
                                }

                            }

                        }
                        else if (PlayerControl.GetHand(PlayerControl.handRight.side).gripPressed)
                        {
                            float distance = Vector3.Distance(item.transform.position, playerRightHand.position);
                            //Debug.Log("distance to right hand: " + Vector3.Distance(item.transform.position, playerRightHand.position));
                            //if Staff is not currently in hand, pull to hand
                            //distance must be greater than 5 to be recalled
                            if (distance > recallMaxDistance && !isRecalling)
                            {
                                isRecalling = true;
                            }
                            else if (distance >= 0.3f && isRecalling)
                            {
                                //turn off Staff while recalling if on and config option is true
                                if (isStaffOn && recallTurnStaffOff)
                                {
                                    if (StaffCycling)
                                    {
                                        StopCoroutine(co);
                                        StaffCycling = false;
                                    }
                                    co = StartCoroutine(ToggleStaff("off", "recall"));
                                }
                                isRecalling = true;
                                Vector3 playerHandPos = playerRightHand.position - item.transform.position;
                                rb.velocity = playerHandPos.normalized * recallStrength;
                            }
                            else if (distance < 0.3f && isRecalling)
                            {
                                //make player grab Staff in hand
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
                        //if Staff is not being recalled but was(ie. dropped before recall finished) set isRecalling back to false and turn damagers back on
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
            //Debug.Log("Changing color of Staff..." + counter);
            switch (counter.ToString())
            {
                case "1":
                    //blue
                    Color blue = new Color(0, 40, 255, 130);
                    Color blueglow = new Color(0, 0, 191);
                    blade.GetComponent<Renderer>().material.SetColor("_Color", blue);
                    blade.GetComponent<Renderer>().material.SetColor("_EmissionColor", blueglow * 0.05f);
                    break;
                case "2":
                    //green
                    Color green = new Color(0, 255, 30, 130);
                    Color greenglow = new Color(0, 191, 0);
                    blade.GetComponent<Renderer>().material.SetColor("_Color", green);
                    blade.GetComponent<Renderer>().material.SetColor("_EmissionColor", greenglow * 0.03f);
                    break;
                case "3":
                    //yellow
                    Color yellow = new Color(255, 229, 0, 130);
                    Color yellowglow = new Color(191, 173, 0);
                    blade.GetComponent<Renderer>().material.SetColor("_Color", yellow);
                    blade.GetComponent<Renderer>().material.SetColor("_EmissionColor", yellowglow * 0.03f);
                    break;
                case "4":
                    //pruple
                    Color purple = new Color(152, 0, 255, 130);
                    Color purpleglow = new Color(60, 0, 191);
                    blade.GetComponent<Renderer>().material.SetColor("_Color", purple);
                    blade.GetComponent<Renderer>().material.SetColor("_EmissionColor", purpleglow * 0.05f);
                    break;
                case "5":
                    Color red = new Color(255, 0, 0, 130);
                    Color redglow = new Color(191, 0, 0);
                    blade.GetComponent<Renderer>().material.SetColor("_Color", red);
                    blade.GetComponent<Renderer>().material.SetColor("_EmissionColor", redglow * 0.05f);
                    break;
                case "6":
                    Color white = new Color(255, 255, 255, 130);
                    Color whiteglow = new Color(191, 191, 191);
                    blade.GetComponent<Renderer>().material.SetColor("_Color", white);
                    blade.GetComponent<Renderer>().material.SetColor("_EmissionColor", whiteglow * 0.03f);
                    //if Staff is double bladed
                    //if (StaffBlades.Count > 1)
                    //{
                    //    //and blade changing is second blade, reset counter to 0
                    //    if (StaffBlades.IndexOf(blade) == 1)
                    //    {
                    //        clickCounter = 0;
                    //    }
                    //}
                    //else
                    //{
                        clickCounter = 0;
                    //}
                    break;
            }
            yield return new WaitForSeconds(0.25f);
            colorCycling = false;
        }

        private IEnumerator ToggleStaff(string state, string reason)
        {
            StaffCycling = true;
            if (state == "on")
            {
                //play ignition sound of Staff, then start timer, when elapsed play hum sound
                isStaffOn = true;
                ElectricityEmmiter.SetActive(true);
                tip3.SetActive(false);
                tip4.SetActive(false);
                tip1.SetActive(true);
                tip2.SetActive(true);
                whoosh.GetComponent<WhooshPoint>().minVelocity = 2.0f;
                whoosh.GetComponent<WhooshPoint>().maxVelocity = 14.0f;
                ignitionOnSound.Play();
                idleSound.Play();
                yield return new WaitForSeconds(ignitionDelay);
                StaffCycling = false;
                StaffDropped = false;
            }
            else
            {
                //turn lightStaff off and stop idle and play ignitionOffSound
                if(reason == "drop")
                {
                    StaffDropped = true;
                    //if dropped/let go, wait 3 seconds before turning off
                    yield return new WaitForSeconds(2.0f);
                }
                isStaffOn = false;
                idleSound.Stop();
                ignitionOffSound.Play();
                //set whooshes to unreachable velocity so the do not play when Staff is turned off
                whoosh.GetComponent<WhooshPoint>().minVelocity = 9999.0f;
                whoosh.GetComponent<WhooshPoint>().maxVelocity = 9999.0f;
                ElectricityEmmiter.SetActive(false);
                tip1.SetActive(false);
                tip2.SetActive(false);
                tip3.SetActive(true);
                tip4.SetActive(true);
                yield return new WaitForSeconds(ignitionDelay);
                StaffCycling = false;
                StaffDropped = false;
            }
        }
    }
}
