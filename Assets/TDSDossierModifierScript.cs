using System;
using System.Reflection;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TDSDossierModifierScript : MonoBehaviour
{
	public KMBombModule Module;
	public Renderer ModuleImageRenderer;
	public Texture SolvedTexture;

	private static int midcount;
	private int mid;

	private static object CurrentMenuPage;
	private static object CurrentEntry;
	private static Queue<TDSDossierModifierScript> UnsolvedModules = new Queue<TDSDossierModifierScript>();
	private static int NumModulesActive;

	void Start()
	{
		mid = ++midcount;

		Module.OnActivate += Activate;
	}

	private bool Activated;

	private void Activate()
	{
		UnsolvedModules.Enqueue(this);

		Activated = true;
		NumModulesActive++;

		Log("Initializing...");

		if (CurrentEntry == null)
		{
			Type sceneManagerType = ReflectionHelper.FindType("SceneManager", "Assembly-CSharp");
			object sceneManager = sceneManagerType.GetProperty("Instance", BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public).GetValue(null, null);
			object gameplayState = sceneManagerType.GetProperty("GameplayState", BindingFlags.Instance | BindingFlags.Public).GetValue(sceneManager, null);
			object gameplayRoom = ReflectionHelper.FindType("GameplayState", "Assembly-CSharp").GetProperty("Room", BindingFlags.Instance | BindingFlags.Public).GetValue(gameplayState, null);
			object mainMenu = ReflectionHelper.FindType("Room", "Assembly-CSharp").GetProperty("MainMenu", BindingFlags.Instance | BindingFlags.Public).GetValue(gameplayRoom, null);
			CurrentMenuPage = ReflectionHelper.FindType("MainMenu", "Assembly-CSharp").GetProperty("GameplayMenuPage", BindingFlags.Instance | BindingFlags.Public).GetValue(mainMenu, null);

			Log("Acquired gameplay dossier page: \"{0}\"", CurrentMenuPage);

			CurrentEntry = ReflectionHelper.FindType("Assets.Scripts.DossierMenu.MenuPage", "Assembly-CSharp").GetMethod("AddEntry", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(CurrentMenuPage, new object[] { "SolveDossierModifier" + mid, null, Delegate.CreateDelegate(ReflectionHelper.FindType("Selectable", "Assembly-CSharp").GetField("OnInteract", BindingFlags.Instance | BindingFlags.Public).FieldType, this, GetType().GetMethod("SolveAll", BindingFlags.NonPublic | BindingFlags.Instance)), "Solve \"Dossier Modifier\"" });
		}
		else SetCurrentEntryHidden(false);
	}

	private void SetCurrentEntryHidden(bool hidden)
	{
		try
		{
			ReflectionHelper.FindType("MenuEntry", "Assembly-CSharp").GetField("IsHidden", BindingFlags.Instance | BindingFlags.Public).SetValue(CurrentEntry, hidden);
			ReflectionHelper.FindType("Assets.Scripts.DossierMenu.MenuPage", "Assembly-CSharp").GetMethod("RefreshLayout", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(CurrentMenuPage, null);
		}
		catch (Exception e)
		{
			Log("Yikes, something went wrong trying to {0} CurrentEntry: {1}", hidden ? "hide" : "show", e.Message);
		}
	}

	private bool SolveAll()
	{
		if (UnsolvedModules.Count > 0)
			UnsolvedModules.Dequeue().Solve();

		if (UnsolvedModules.Count == 0)
		{
			SetCurrentEntryHidden(true);
			CurrentEntry = null;
		}
		return false;
	}

	private bool IsSolved;

	private void Solve()
	{
		if (!IsSolved)
		{
			IsSolved = true;
			ModuleImageRenderer.material.mainTexture = SolvedTexture;
			Log("Module solved.");
			Module.HandlePass();
		}
	}

	private void Log(string format, params object[] args)
	{
		Debug.LogFormat("[Dossier Modifier #{0}] {1}", mid, string.Format(format, args));
	}

	void OnDestroy()
	{
		if (Activated) NumModulesActive--;
		if (UnsolvedModules.Contains(this)) UnsolvedModules = new Queue<TDSDossierModifierScript>(UnsolvedModules.Where(m => m != this));
		if (NumModulesActive == 0)
		{
			if (CurrentEntry != null) SetCurrentEntryHidden(true);
			CurrentEntry = null;
		}
	}
}
