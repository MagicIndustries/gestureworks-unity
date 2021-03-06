﻿////////////////////////////////////////////////////////////////////////////////
//
//  IDEUM
//  Copyright 2011-2013 Ideum
//  All Rights Reserved.
//
//  Gestureworks Unity
//
//  File:    GestureWorksUnity.cs
//  Authors:  Ideum
//
//  NOTICE: Ideum permits you to use, modify, and distribute this file only
//  in accordance with the terms of the license agreement accompanying it.
//
////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using GestureWorksCoreNET;
using GestureWorksCoreNET.Unity;

#if UNITY_EDITOR
	using UnityEditor;
#endif

public class GestureWorksUnity 
{	
	private string dllFileName = "GestureworksCore32.dll";
	public string DllFileName
	{
		get { return dllFileName; }
		set { dllFileName = value; }	
	}
	
	private string dllFilePathEditor = @"\Gestureworks\Core\";
	public string DllFilePathEditor
	{
		get { return dllFilePathEditor; }
		set { dllFilePathEditor = value; }
	}
	
	private string gmlFileName = "my_gestures.gml";
	public string GmlFileName
	{
		get { return gmlFileName; }
		set { gmlFileName = value; }
	}
	
	private string gmlFilePathEditor = @"\Gestureworks\Core\";
	public string GmlFilePathEditor
	{
		get { return gmlFilePathEditor; }
		set { gmlFilePathEditor = value; }	
	}
	
	private string dllFilePath;
	private string gmlFilePath;
	
	public string ApplicationDataPath
	{
		get
		{
			return Application.dataPath.Replace("/", "\\");	
		}
	}
	
	private static GestureWorksUnity instance = null;
	public static GestureWorksUnity Instance
	{
		get
		{
			if(instance == null)
			{
				instance = new GestureWorksUnity();	
			}
			
			return instance;
		}
	}
	
	private bool initialized = false;
	public bool Initialized
	{
		get { return initialized; }	
	}
	
	private string initializationError = "";
	public string InitializationError
	{
		get 
		{
			if(string.IsNullOrEmpty(initializationError))
			{
				return "";
			}
			
			return "GestureWorks initialization error:\n" + initializationError; 
		}	
	}
	
	private volatile bool loaded = false;
	public bool Loaded
	{
		get { return loaded; }
		set { loaded = value; }
	}
	
	private volatile bool processingGestures = false;
	public bool ProcessingGestures
	{
		get { return processingGestures; }
		set { processingGestures = value; }
	}
	
	private volatile bool pauseGestureProcessing = false;
	public bool PauseGestureProcessing
	{
		get { return pauseGestureProcessing; }
		set { pauseGestureProcessing = value; }
	}
	
	private bool mouseSimEnabled = false;
	public bool MouseSimEnabled
	{
		get { return mouseSimEnabled; }	
	}
	
	private GestureWorksUnityMouseSim mouseSimulator = new GestureWorksUnityMouseSim();
	
	private bool forceMouseSimEnabled = false;
	public bool ForceMouseSimEnabled
	{
		get { return forceMouseSimEnabled; }
		set { forceMouseSimEnabled = value; }
	}
	
	public float MouseTwoPointSimStartDistance
	{
		get { return mouseSimulator.MouseTwoPointSimStartDistance; }
		set { mouseSimulator.MouseTwoPointSimStartDistance = value; }	
	}
	
	public bool ShowMousePoints
	{
		get { return mouseSimulator.ShowMousePoints; }
		set { mouseSimulator.ShowMousePoints = value; }
	}
	
	public bool ShowMouseEventInfo
	{
		get { return mouseSimulator.ShowMouseEventInfo; }
		set { mouseSimulator.ShowMouseEventInfo = value; }
	}
	
	private bool showTouchPoints = true;
	public bool ShowTouchPoints
	{
		get 
		{
			if(MouseSimEnabled)
			{
				return false;	
			}
			
			return showTouchPoints; 
		}
		set { showTouchPoints = value; }
	}
	
	private bool showTouchEventInfo = false;
	public bool ShowTouchEventInfo
	{
		get { return showTouchEventInfo; }
		set { showTouchEventInfo = value; }
	}
	
	private string CurrentSceneName
	{
		get
		{
#if UNITY_EDITOR
			
			string[] scenePathParts = EditorApplication.currentScene.Split(char.Parse("/"));
			
			if(scenePathParts.Length <= 0)
			{
				Debug.LogWarning("Could not find current scene name");	
				
				return "";
			}
			
			string sceneName = scenePathParts[scenePathParts.Length - 1];
			
			if(string.IsNullOrEmpty(sceneName))
			{
				return "Untitled";	
			}
			
			return sceneName;
#else
			return Application.loadedLevelName;
#endif
		}
	}
	
	private string ApplicationName
	{
		get
		{
			string[] applicationNameParts = Application.dataPath.Split(char.Parse("/"));
			if(applicationNameParts.Length <= 1)
			{
				Debug.LogWarning("Could not find application name");
				
				return "";
			}
			
			return applicationNameParts[applicationNameParts.Length - 2];
		}
	}
	
	private string CurrentSceneNameNoExtension
	{
		get
		{
			string name = CurrentSceneName;
			int unityIndex = CurrentSceneName.IndexOf(".unity");
			if(unityIndex >= 0)
			{
				return name.Remove(unityIndex);	
			}
			else
			{
				return name;	
			}
		}
	}
	
	private string EditorWindowName
	{
		get
		{
			return "Unity - " + CurrentSceneName + " - " + ApplicationName + " - PC, Mac & Linux Standalone*";	
		}
	}
	
	public string GameWindowName
	{
		get
		{
#if UNITY_EDITOR
			return PlayerSettings.productName;
#else
			return GestureWorksConfiguration.Instance.KeyValue("ProductName");
#endif
		}
	}
	
	private Camera gameCamera = null;
	public Camera GameCamera
	{
		set { gameCamera = value; }
		get
		{
			if(gameCamera != null)
			{
				return gameCamera;	
			}
			
			return Camera.main;
		}
	}
	
	private string GameCameraGestureObjectName
	{
		get
		{
			Camera camera = GameCamera;
			if(camera == null)
			{
				return "";	
			}
			
			GameObject cameraObject = camera.gameObject;
			TouchObject touch = cameraObject.GetComponent<TouchObject>();
			if(touch == null)
			{
				return "";
			}
			
			return touch.GestureObjectName;
		}
	}
	
	private float timeSinceLastEvent = 0.0f;
	public float TimeSinceLastEvent
	{
		get { return timeSinceLastEvent; }	
	}
	
	public void ResetTimeSinceLastEvent()
	{
		timeSinceLastEvent = 0.0f;	
	}
	
	private string windowName = "";
	
	private List<TouchObject> gestureObjects = new List<TouchObject>();
	
	private GestureWorks gestureWorksCore;
	
	private GestureEventArray gestureEvents = null;
	
	private PointEventArray pointEvents = null;
	
	private Dictionary<int, TouchCircle> touchCircles = new Dictionary<int, TouchCircle>();
	
	private HitManager hitManager;
	
	public bool EscapeKeyExitApplication { get; set; }
	
	public bool LogInitialization { get; set; }
	
	public bool LogInputEnabled { get; set; }
	
	private GestureWorksUnity()
	{
		
	}
	
	public void Initialize()
	{
		if(initialized)
		{
			Debug.LogWarning("Initialize being called more than once on GestureWorksUnity");
			
			return;
		}
		
		GestureWorksConfiguration.Instance.Initialize();
		
		if(!FindSetupInfo())
		{
			return;
		}
		
		if(!InitializeGestureWorks())
		{
			return;	
		}
		
		initialized = true;
		
		RegisterTouchObjects();
	}
	
	private bool FindSetupInfo()
	{	
		if(Application.isEditor)
		{
			mouseSimEnabled = true;
			windowName = EditorWindowName;
			
			Debug.Log("== NOTE: To touch the game, you must build and run ==");
			
			dllFilePath = ApplicationDataPath + DllFilePathEditor + DllFileName;
			gmlFilePath = ApplicationDataPath + GmlFilePathEditor + GmlFileName;
		} 
		else 
		{
			// Running exe 
			mouseSimEnabled = ForceMouseSimEnabled;
			windowName = GameWindowName;
			dllFilePath = ApplicationDataPath + "\\" + DllFileName;
			gmlFilePath = ApplicationDataPath + "\\" + GmlFileName;
		}
		
		if(!File.Exists(dllFilePath))
		{
			initializationError = "Could not find dll at " + dllFilePath;
			Debug.LogError(initializationError + " Stopping GestureWorks Initialization");
			return false;
		}
		
		if(!File.Exists(gmlFilePath))
		{
			initializationError = "Could not find gml at " + dllFilePath;
			Debug.LogError(initializationError + " Stopping GestureWorks Initialization");
			return false;
		}
		
		if(LogInitialization)
		{
			Debug.Log("DLL file path: " + dllFilePath);
			Debug.Log("GML file path: " + gmlFilePath);
		}
		
		return true;
	}
	
	private bool InitializeGestureWorks()
	{	
		gestureWorksCore = new GestureWorks();
		
		bool dllLoaded = gestureWorksCore.LoadGestureWorksDll(dllFilePath);
		
		if(!dllLoaded)
		{
			initializationError = "Could not load dll " + dllFilePath;
			Debug.LogError(initializationError + " Stopping GestureWorks Initialization");	
			return false;
		}
		
		gestureWorksCore.InitializeGestureWorks(Screen.width, Screen.height);
		
		bool gmlLoaded = gestureWorksCore.LoadGML(gmlFilePath);
		
		if(!gmlLoaded)
		{
			initializationError = "Could not load gml " + gmlFilePath ;
			Debug.LogError(initializationError + " Stopping GestureWorks Initialization");	
			return false;
		}
		
		bool windowLoaded = gestureWorksCore.RegisterWindowForTouchByName(windowName);
        if (!windowLoaded)
        {
            windowLoaded = gestureWorksCore.RegisterWindowForTouch(gestureWorksCore.GetActiveWindow());
        }
		
		hitManager = new HitManager();
		
		mouseSimulator.Initialize(gestureWorksCore);
		
		Debug.Log("Success initializing GestureWorks");
		
		return true;
	}
	
	public void RegisterTouchObjects()
	{	
		if(!initialized)
		{
			Debug.LogWarning("Trying to register touch objects when GestureWorksUnity has not been initialized");
			return;
		}
		
		int currentTouchObjectCount = gestureObjects.Count;
		
		TouchObject[] touchObjects = GameObject.FindObjectsOfType(typeof(TouchObject)) as TouchObject[];
		foreach(TouchObject obj in touchObjects)
		{
			RegisterTouchObject(obj);
		}
		
		if(LogInitialization)
		{
			Debug.Log("RegisterGestureObjects found " + (gestureObjects.Count - currentTouchObjectCount) + " touch objects");
		}
		
		loaded = true;
	}
	
	public bool RegisterTouchObject(TouchObject obj)
	{
		if(!initialized)
		{
			Debug.LogWarning("Trying to register touch object when GestureWorksUnity has not been initialized");
			return false;
		}
		
		if(obj == null)
		{
			return false;	
		}
		
		if(LogInitialization)
		{
			Debug.Log("Touch object " + obj.gameObject.name + " found.");
		}
		
		gestureWorksCore.RegisterTouchObject(obj.GestureObjectName);
		
		foreach(string gesture in obj.SupportedGestures)
		{
			if(LogInitialization)
			{
				Debug.Log("	Object has gesture " + gesture);	
			}
			
			gestureWorksCore.AddGesture(obj.GestureObjectName, gesture);
		}
		
		gestureObjects.Add(obj);
		
		return true;
	}
	
	public void DeregisterAllTouchObjects()
	{
		TouchObject[] touchObjects = GameObject.FindObjectsOfType(typeof(TouchObject)) as TouchObject[];
		foreach(TouchObject obj in touchObjects)
		{
			DeregisterTouchObject(obj);
		}
	}
	
	public void DeregisterTouchObject(TouchObject obj)
	{
		gestureWorksCore.DeregisterTouchObject(obj.GestureObjectName);
		
		foreach(string gesture in obj.SupportedGestures)
		{	
			gestureWorksCore.DisableGesture(obj.GestureObjectName, gesture);
			gestureWorksCore.RemoveGesture(obj.GestureObjectName, gesture);
		}
		
		gestureObjects.Remove(obj);
	}
	
	public void ClearTouchPoints()
	{
		foreach(TouchCircle circle in touchCircles.Values)
		{
			circle.RemoveRing();	
		}
		
		touchCircles.Clear();
		
		mouseSimulator.ClearMousePoints();
	}
	
	public void Update(float deltaTime)
	{
		if(EscapeKeyExitApplication && Input.GetKeyDown(KeyCode.Escape))
		{
			Application.Quit();	
		}
		
		if(!initialized || !loaded || pauseGestureProcessing)
		{
			return;
		}
		
		processingGestures = true;
		
		gestureWorksCore.ProcessFrame();
		
		pointEvents = gestureWorksCore.ConsumePointEvents();
		
		if(mouseSimEnabled)
		{
			UpdateMouseEvents();	
		}
		
		if(LogInputEnabled)
		{
			LogPoints();	
		}
		
		UpdateTouchPoints();
		
		gestureEvents = gestureWorksCore.ConsumeGestureEvents();
		
		UpdateGestureEvents();
		
		if((pointEvents == null || pointEvents.Count == 0) &&
			(gestureEvents == null || gestureEvents.Count == 0))
		{
			timeSinceLastEvent += deltaTime;	
		}
		else
		{
			timeSinceLastEvent = 0.0f;	
		}
		
		processingGestures = false;
	}
	
	private void UpdateMouseEvents()
	{
		if(!initialized || !mouseSimEnabled)
		{
			return;	
		}
		
		mouseSimulator.Update();
	}
	
	private void LogPoints()
	{
		if(pointEvents == null || !LogInputEnabled)
		{
			return;	
		}
		
		foreach (PointEvent pEvent in pointEvents)
		{
			if (pEvent.Status == TouchStatus.TOUCHADDED)
			{
				string output = "TOUCHADDED-----------------------------\r\n";
				output += "Point ID:  " +    		pEvent.PointId.ToString();
				output += "\r\n X: " +           	pEvent.Position.X.ToString();
				output += "\r\n Y: " +            	pEvent.Position.Y.ToString();
				output += "\r\n W: " +           	pEvent.Position.W.ToString();
				output += "\r\n H: " +            	pEvent.Position.H.ToString();
				output += "\r\n Z: " +            	pEvent.Position.Z.ToString();
				output += "\r\n Touch Status: " + 	pEvent.Status.ToString();
				output += "\r\n Timestamp: \r\n" +  pEvent.Timestamp.ToString();
				
				Debug.Log(output);
			}
		}
	}
	
	private void UpdateTouchPoints()
	{
		if(pointEvents == null)
		{
			return;
		}
		
		foreach(PointEvent pEvent in pointEvents)
		{
			switch(pEvent.Status)
			{
				
			case TouchStatus.TOUCHADDED:
			{
				if(ShowTouchPoints &&
					!touchCircles.ContainsKey(pEvent.PointId))
				{
					touchCircles.Add(pEvent.PointId, 
									new TouchCircle(pEvent.PointId,
													ShowTouchEventInfo,
													pEvent.Position.X, pEvent.Position.Y));	
				}
				
				string hitObjectName = "";
				bool hitSomething = hitManager.DetectHitSingle(pEvent.Position.X,
															   Screen.height - pEvent.Position.Y, 
															   out hitObjectName);
				
				
				bool touchPointHitSomething = false;
				if(hitSomething)
				{
					if(LogInputEnabled)
					{
						Debug.Log("Hit " + hitObjectName);
					}
					
					foreach(TouchObject obj in gestureObjects)
					{
						if(obj.GestureObjectName == hitObjectName)
						{
							if(LogInputEnabled)
							{
								Debug.Log("Adding touch point to " + obj.GestureObjectName);
							}
							
							gestureWorksCore.AddTouchPoint(obj.GestureObjectName, pEvent.PointId);
							
							touchPointHitSomething = true;
							
							break;
						}
					}
				}
					
				if(!touchPointHitSomething)
				{
					string cameraGestureName = GameCameraGestureObjectName;
					if(!string.IsNullOrEmpty(cameraGestureName))
					{
						gestureWorksCore.AddTouchPoint(cameraGestureName, pEvent.PointId);
					}
				}
			}
				break;
				
			case TouchStatus.TOUCHREMOVED:
			{
				if(ShowTouchPoints && touchCircles.ContainsKey(pEvent.PointId))
				{
					touchCircles[pEvent.PointId].RemoveRing();
					touchCircles.Remove(pEvent.PointId);
				}
			}
				break;
				
			case TouchStatus.TOUCHUPDATE:
			{
				if(ShowTouchPoints && touchCircles.ContainsKey(pEvent.PointId))
				{
					touchCircles[pEvent.PointId].Update(pEvent.Position.X, pEvent.Position.Y);
				}
			}
				break;
				
			default:
				break;
				
			}
		}
	}
	
	private void UpdateGestureEvents()
	{
		if(gestureEvents == null)
		{
			return;
		}
		
		foreach (GestureEvent gEvent in gestureEvents)
		{
			if(LogInputEnabled)
			{
		        string o = "-----------------------------\r\n";
		        o += gEvent.ToString();
		        o += "EventID: "   + gEvent.EventID;
		        o += "\r\n GestureID: " + gEvent.GestureID;
		        o += "\r\n Target: "    + gEvent.Target;
		        o += "\r\n N: "         + gEvent.N.ToString();
		        o += "\r\n X: "         + gEvent.X.ToString();
		        o += "\r\n Y: "         + gEvent.Y.ToString();
		        o += "\r\n Timestamp: " + gEvent.Timestamp.ToString();
		        o += "\r\n Locked Points: " + gEvent.LockedPoints.GetLength(0).ToString();
				o += "\r\n";
						
				Debug.Log(o);
			}
					
			// send gesture events to all subscribers
			foreach(TouchObject obj in gestureObjects)
			{
				//send events only to corresponding registered touch objects
				if(obj.GestureObjectName == gEvent.Target)
				{
					obj.SendMessage(gEvent.GestureID, gEvent);
				}
			}
		}
	}
}
