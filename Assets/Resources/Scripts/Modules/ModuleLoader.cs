using System;
using System.IO;
using System.Linq;
using Jurassic.Library;
using UnityEngine;

/// <summary>
/// The ModuleLoader class contains static functions to load 
/// all modules for the game. This means all 
/// actors, blocks, entities, logical blocks, 
/// and other items will be loaded into the game
/// by these functions.
/// </summary>
public class ModuleLoader
{
	public const string MAIN_SCRIPT_RELATIVE_PATH = "main.js";
	public const string MAIN_OBJECT_NAME = "mainObject";

	public const string CUBE_MESH_PATH = "Models/cube";

	public static Mesh cubeMesh;

    public static char GetOSDirectorySeparator()
    {
        bool isWindows =
            Application.platform == RuntimePlatform.WindowsPlayer ||
            Application.platform == RuntimePlatform.WindowsEditor ||
            Application.platform == RuntimePlatform.WindowsWebPlayer;

        //return isWindows ? "\\" : "/";
        return '\\';
    }

    public static void LoadModules(string path)
	{
		cubeMesh = Resources.Load<Mesh>(CUBE_MESH_PATH);

		foreach (string dir in Directory.GetDirectories(path))
		{
			if (dir.Contains(".git")) continue;
			LoadModule(dir);
		}
	}

	// Loads a module at a given path
	private static void LoadModule(string path)
	{
		// All actor type directories in a module (entities, blocks...)
		foreach (string dir in Directory.GetDirectories(path))
		{

			LoadActors(dir);
		}
	}

	// Loads all the actors from a type of actor's directory.
	// i.e. Load all blocks from the block directory
	private static void LoadActors(string path)
	{
		string type = path.Split(GetOSDirectorySeparator()).Last().ToLower();

		// All actor directories within an actor type directory
		foreach (string dir in Directory.GetDirectories(path))
		{
			LoadActor(dir, type);
		}
	}

	// Loads an actor based on its directory
	private static void LoadActor(string dir, string type)
	{
		RunScript(dir + GetOSDirectorySeparator() + MAIN_SCRIPT_RELATIVE_PATH);
		StoreActorData(dir, type);
	}

	private static void RunScript(string path)
	{
		JSMaster.ExecuteFile(path);
	}

	private static void StoreActorData(string path, string type)
	{
		// Store data of block including the id and path to js file to run when spawning
		string name = path.Split(GetOSDirectorySeparator()).Last().ToLower();

		var blockData = GetActorData(type, name, JSMaster.actorStore.Count, path + GetOSDirectorySeparator() + MAIN_SCRIPT_RELATIVE_PATH);

		// Old list
		//JSMaster.actorStore.Add(blockData);

		// New dictionary. Gives id from list of keys, name as key.
		JSMaster.actorStore.Add(name.ToLower(), blockData);
	}

	private static ActorData GetActorData(string type, string name,
	                                      int id, string path)
	{
		switch (type)
		{
			case "blocks":
				return new BlockData(name, JSMaster.actorStore.Count, path);
			default:
				return new ActorData(name, JSMaster.actorStore.Count, path);
		}
	}
}