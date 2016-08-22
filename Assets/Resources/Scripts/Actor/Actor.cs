using System;
using System.Collections;
using UnityEngine;

using Jurassic;
using Jurassic.Library;
using JSUtil;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

public class Actor : MonoBehaviour
{
	public ObjectInstance jsObject;

	public static int lastId = 0;

	protected int id;

	private const float TICK_DELAY = 0.5f;

	#region JS Function Name Constants

	private const string JS_AWAKE_NAME = "awake";
	private const string JS_TICK_NAME = "tick";
	private const string JS_ACTIVATE_NAME = "activate";

	#endregion

	// Stores all possible directions for an actor to point at.
	public Dictionary<string, Vector3> dirs = new Dictionary<string, Vector3>();

	public static Actor Spawn(string actorName, Vector3 position)
	{
		return JSMaster.actorStore[actorName].Spawn(position);
	}

	public static Actor Spawn(ActorData data, Vector3 position)
	{
		GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
		cube.transform.position = position;

		Actor actor = cube.AddComponent<Actor>();

		actor.name = data.name;

		actor.id = lastId++;

		actor.SetupJSObject(data);

		SetMesh(actor, ModuleLoader.cubeMesh);

		SetTextureFromFile(data, actor);

		return actor;
	}

	protected static void SetMesh(Actor actor, Mesh mesh)
	{
		actor.GetComponent<MeshFilter>().mesh = mesh;
	}

	protected static void SetTextureFromFile(ActorData data, Actor actor)
	{
		var pathArray = data.path.Split(ModuleLoader.GetOSDirectorySeparator());

		pathArray[pathArray.Length - 1] = "texture.png";

		var texture = ReadTexture(string.Join(ModuleLoader.GetOSDirectorySeparator().ToString(), pathArray));

		actor.GetComponent<Renderer>().material.SetTexture("_MainTex", texture);
	}

	protected static Texture ReadTexture(string path)
	{
		byte[] imageBytes = File.ReadAllBytes(path);

		Texture2D texture = new Texture2D(2, 2);

		texture.LoadImage(imageBytes);

		return texture;
	}

	public void SetupJSObject(ActorData data)
	{
		RefreshJSObject(data.path);

		SetJSMemberFunctions();

		jsAwake();

		StartTicking();
	}

	protected virtual void Awake()
	{
		gameObject.layer = LayerMask.NameToLayer(LayerHelper.ACTOR_LAYER);
		AddDirectionsToDictionary();
	}

	protected virtual void Start()
	{ 
	}

	protected virtual void Update()
	{
	}

	public void StartTicking()
	{
		if (jsObject.HasProperty(JS_TICK_NAME))
		{
			StartCoroutine(RunTick());
		}
	}

	// Runs the tick of every actor constantly.
	// Function does not reach its end or a 'return' statement by any of possible execution paths
	#pragma warning disable RECS0135
	protected IEnumerator RunTick()
	{
		yield return new WaitForSeconds(UnityEngine.Random.value * TICK_DELAY);

		while (true)
		{
			Tick();
			float delay = TICK_DELAY / 2;
			yield return new WaitForSeconds(delay);
		}
	}

	#region JavaScript API Functions

	//Creates a JS Vector using transform position
	public JSVectorInstance jsGetPos() { 
		return new JSVectorConstructor(JSMaster.engine).Construct(
			(double)transform.position.x,
			(double)transform.position.y,
			(double)transform.position.z);
	}

	public void jsSetPos(double x, double y, double z)
	{
		transform.position = new Vector3((float)x, (float)y, (float)z);
	}

	#endregion

	#region JS API Wrappers

	public void jsAwake()
	{
		if (!jsObject.HasProperty(JS_AWAKE_NAME)) return;

		jsObject.CallMemberFunction(JS_AWAKE_NAME);
	}

	public void Tick()
	{
		jsObject.CallMemberFunction(JS_TICK_NAME);
	}

	public void Activate()
	{
			if (!jsObject.HasProperty(JS_ACTIVATE_NAME)) return;

		jsObject.CallMemberFunction(JS_ACTIVATE_NAME);
	}

	#endregion

	#region JS Helpers

	protected virtual ObjectInstance GetJSObject()
	{
		return jsObject;
	}

	protected void RefreshJSObject(string path)
	{
		// if we execute the script that belongs to this type, we can grab the correct mainobject.
		JSMaster.ExecuteFile(path);

		// Get main object created by modder
		var mainObject = JSMaster.engine.GetGlobalValue<ObjectInstance>("mainObject");

		// Set the name to be this thing's name + its id as a unique identifier
		JSMaster.engine.SetGlobalValue(gameObject.name + id, mainObject);

		// Store a reference to the unique identifier once
		jsObject = (ObjectInstance)JSMaster.engine.Global[gameObject.name + id];
	}


	protected virtual void SetJSMemberFunctions()
	{
	}

	#endregion


	#region Various Helper Methods

	private void AddDirectionsToDictionary()
	{
		dirs.Add("forward", transform.forward);
		dirs.Add("backward", -transform.forward);
		dirs.Add("right", transform.right);
		dirs.Add("left", -transform.right);
		dirs.Add("down", -transform.up);
		dirs.Add("up", transform.up);
	}

	public T GetOrAddComponent<T>() where T : Component
	{
		return GetComponent<T>() ? GetComponent<T>() : gameObject.AddComponent<T>();
	}

	#endregion
}

/// <summary>
/// The LogicalBlockData struct represents the data of a certain logical block type.
/// For example, if the user wanted to spawn a generator, the generator LogicalBlockData
/// could be fetched from memory by ID, and then an instance of a generator
/// may be spawned using the fetched data.
/// </summary>
public class ActorData
{
	public string name;
	public int id;
	public string path;

	public ActorData(string name, int id, string path)
	{
		this.name = name;
		this.id = id;
		this.path = path;
	}

	public virtual Actor Spawn(Vector3 position)	
	{
		return Actor.Spawn(this, position);
	}
}

public class BlockData : ActorData
{
	public BlockData(string name, int id, string path) : base(name, id, path)
	{
	}
	
	public override Actor Spawn(Vector3 position)
	{
		return Block.Spawn(this, position);
	}
}
		