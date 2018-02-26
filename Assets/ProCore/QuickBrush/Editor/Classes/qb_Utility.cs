//	QuickBrush: Prefab Placement Tool
//	by PlayTangent
//	all rights reserved
//	www.ProCore3d.com

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

// save as .qbt special binary
public static class qb_Utility
{
	public static string GetHeadDirectory()
	{
		if ( Directory.Exists("Assets/ProCore/QuickBrush") )
		{
			return "Assets/ProCore/QuickBrush";
		}
		else
		{
			string[] matches = Directory.GetDirectories("Assets/", "QuickBrush", SearchOption.AllDirectories);

			if (matches != null && matches.Length > 0)
				return matches[0];
			else
				Debug.LogError("QuickBrush directory could not be found!  If you've moved it make sure the main folder is still named \"QuickBrush\"");

			return "Assets/";
		}
	}

	public static void SaveToDisk(qb_Template template, string directory) // Save the current brush to memory
	{
		string fileName = directory + "/Templates/" + template.brushName + ".qbt";

		Stream stream = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
		//Debug.Log("SavingTemplate");
		Hashtable propertyTable = CreatePropertyTable(template);

		BinaryFormatter formatter = new BinaryFormatter();
		formatter.Serialize(stream, propertyTable);

		stream.Close();
	}

	static Hashtable CreatePropertyTable(qb_Template template)	//converts brush class to a hash table of values
	{
		Hashtable propertyTable = new Hashtable();

		propertyTable.Add("BrushName", template.brushName);

		propertyTable.Add("LastKnownAs", template.lastKnownAs);

		#region Brush Settings Vars
		propertyTable.Add("BrushRadius", template.brushRadius);

		propertyTable.Add("BrushRadiusMin", template.brushRadiusMin);

		propertyTable.Add("BrushRadiusMax", template.brushRadiusMax);

		propertyTable.Add("BrushSpacing", template.brushSpacing);

		propertyTable.Add("BrushSpacingMin", template.brushSpacingMin);

		propertyTable.Add("BrushSpacingMax", template.brushSpacingMax);

		propertyTable.Add("ScatterRadius", template.scatterRadius);
		#endregion

		#region Rotation Settings Vars
		propertyTable.Add("AlignToNormal", template.alignToNormal);

		propertyTable.Add("FlipNormalAlign", template.flipNormalAlign);

		propertyTable.Add("AlignToStroke", template.alignToStroke);

		propertyTable.Add("FlipStrokeAlign", template.flipStrokeAlign);

		propertyTable.Add("RotationRangeMinX", template.rotationRangeMin.x);
		propertyTable.Add("RotationRangeMinY", template.rotationRangeMin.y);
		propertyTable.Add("RotationRangeMinZ", template.rotationRangeMin.z);

		propertyTable.Add("RotationRangeMaxX", template.rotationRangeMax.x);
		propertyTable.Add("RotationRangeMaxY", template.rotationRangeMax.y);
		propertyTable.Add("RotationRangeMaxZ", template.rotationRangeMax.z);
		#endregion

		#region Position Settings Vars
		propertyTable.Add("PositionOffsetX", template.positionOffset.x);
		propertyTable.Add("PositionOffsetY", template.positionOffset.y);
		propertyTable.Add("PositionOffsetZ", template.positionOffset.z);
		#endregion

		#region Scale Settings Vars
		propertyTable.Add("ScaleAbsolute", template.scaleAbsolute);

		//The minimum and maximum possible scale
		propertyTable.Add("ScaleMin", template.scaleMin);
		propertyTable.Add("ScaleMax", template.scaleMax);

		//The minimum and maximum current scale range setting
		propertyTable.Add("ScaleRandMinX", template.scaleRandMin.x);
		propertyTable.Add("ScaleRandMinY", template.scaleRandMin.y);
		propertyTable.Add("ScaleRandMinZ", template.scaleRandMin.z);

		propertyTable.Add("ScaleRandMaxX", template.scaleRandMax.x);
		propertyTable.Add("ScaleRandMaxY", template.scaleRandMax.y);
		propertyTable.Add("ScaleRandMaxZ", template.scaleRandMax.z);

		propertyTable.Add("ScaleRandMinUniform", template.scaleRandMinUniform);

		propertyTable.Add("ScaleRandMaxUniform", template.scaleRandMaxUniform);

		propertyTable.Add("ScaleUniform", template.scaleUniform);
		#endregion

		#region Sorting Vars
		//Selection
		propertyTable.Add("PaintToSelection", template.paintToSelection);

		//Layers
		propertyTable.Add("PaintToLayer", template.paintToLayer);

		propertyTable.Add("LayerIndex", template.layerIndex);

		propertyTable.Add("GroupObjects", template.groupObjects);

		//propertyTable.Add("GroupIndex",template.groupIndex);
		propertyTable.Add("GroupName", template.groupName);
		#endregion

		#region Eraser Vars
		propertyTable.Add("EraseByGroup", template.eraseByGroup);
		propertyTable.Add("EraseBySelected", template.eraseBySelected);
		#endregion

		//New Prefab Save
		string prefabGroupString = string.Empty;

		for (int i = 0; i < template.prefabGroup.Length; i ++)
		{
			prefabGroupString += "/" + AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(template.prefabGroup[i].prefab)) + "-" + template.prefabGroup[i].weight.ToString();
		}

		propertyTable.Add("PrefabGUIDList", prefabGroupString);

		return propertyTable;
	}



	public static qb_TemplateSignature LoadTemplateSignature(string fileLocationString)
	{
		BinaryFormatter formatter = new BinaryFormatter();
		Stream stream = new FileStream(fileLocationString, FileMode.Open, FileAccess.Read, FileShare.Read);

		Hashtable propertyTable = (Hashtable) formatter.Deserialize(stream);
		stream.Close();

		qb_TemplateSignature signature = new qb_TemplateSignature();

		signature.directory = fileLocationString;
		signature.name = (string) propertyTable["BrushName"];

		return signature;
	}


	public static qb_Template LoadFromDisk(string fileLocationString)
	{
		//brush properties pulled from disk and re-assembled in new qb_Brush class instance
		BinaryFormatter formatter = new BinaryFormatter();
		Stream stream = new FileStream(fileLocationString, FileMode.Open, FileAccess.Read, FileShare.Read);

		Hashtable propertyTable = (Hashtable) formatter.Deserialize(stream);
		stream.Close();

		qb_Template template = new qb_Template();//ScriptableObject.CreateInstance<qb_Template>();//new qb_Template();

		template.brushName =		(string) GetProperty<string>(ref propertyTable, "BrushName", template.brushName);

		template.lastKnownAs =		(string) GetProperty<string>(ref propertyTable, "LastKnownAs", template.lastKnownAs);
		#region Brush Settings Vars
		template.brushRadius =		(float) GetProperty<float>(ref propertyTable, "BrushRadius", template.brushRadius);

		template.brushRadiusMin =	(float) GetProperty<float>(ref propertyTable, "BrushRadiusMin", template.brushRadiusMin);

		template.brushRadiusMax =	(float) GetProperty<float>(ref propertyTable, "BrushRadiusMax", template.brushRadiusMax);

		template.brushSpacing =		(float) GetProperty<float>(ref propertyTable, "BrushSpacing", template.brushSpacing);

		template.brushSpacingMin =	(float) GetProperty<float>(ref propertyTable, "BrushSpacingMin", template.brushSpacingMin);

		template.brushSpacingMax =	(float) GetProperty<float>(ref propertyTable, "BrushSpacingMax", template.brushSpacingMax);

		template.scatterRadius =	(float) GetProperty<float>(ref propertyTable, "ScatterRadius", template.scatterRadius);
		#endregion

		#region Rotation Settings Vars
		template.alignToNormal =	(bool) GetProperty<bool>(ref propertyTable, "AlignToNormal", template.alignToNormal);

		template.flipNormalAlign =	(bool) GetProperty<bool>(ref propertyTable, "FlipNormalAlign", template.flipNormalAlign);

		template.alignToStroke =	(bool) GetProperty<bool>(ref propertyTable, "AlignToStroke", template.alignToStroke);

		template.flipStrokeAlign =	(bool) GetProperty<bool>(ref propertyTable, "FlipStrokeAlign", template.flipStrokeAlign);

		template.rotationRangeMin.x = (float) GetProperty<float>(ref propertyTable, "RotationRangeMinX", template.rotationRangeMin.x);
		template.rotationRangeMin.y = (float) GetProperty<float>(ref propertyTable, "RotationRangeMinY", template.rotationRangeMin.y);
		template.rotationRangeMin.z = (float) GetProperty<float>(ref propertyTable, "RotationRangeMinZ", template.rotationRangeMin.z);

		template.rotationRangeMax.x = (float) GetProperty<float>(ref propertyTable, "RotationRangeMaxX", template.rotationRangeMax.x);
		template.rotationRangeMax.y = (float) GetProperty<float>(ref propertyTable, "RotationRangeMaxY", template.rotationRangeMax.y);
		template.rotationRangeMax.z = (float) GetProperty<float>(ref propertyTable, "RotationRangeMaxZ", template.rotationRangeMax.z);
		#endregion

		#region Position Settings Vars
		template.positionOffset.x =	(float) GetProperty<float>(ref propertyTable, "PositionOffsetX", template.positionOffset.x);
		template.positionOffset.y = (float) GetProperty<float>(ref propertyTable, "PositionOffsetY", template.positionOffset.y);
		template.positionOffset.z = (float) GetProperty<float>(ref propertyTable, "PositionOffsetZ", template.positionOffset.z);
		#endregion

		#region Scale Settings Vars
		template.scaleAbsolute =	(bool) GetProperty<bool>(ref propertyTable, "ScaleAbsolute", template.scaleAbsolute);

		//The minimum and maximum possible scale
		template.scaleMin =			(float) GetProperty<float>(ref propertyTable, "ScaleMin", template.scaleMin);
		template.scaleMax =			(float) GetProperty<float>(ref propertyTable, "ScaleMax", template.scaleMax);

		//The minimum and maximum current scale range setting
		template.scaleRandMin.x =	(float) GetProperty<float>(ref propertyTable, "ScaleRandMinX", template.scaleRandMin.x);
		template.scaleRandMin.y =	(float) GetProperty<float>(ref propertyTable, "ScaleRandMinY", template.scaleRandMin.y);
		template.scaleRandMin.z =	(float) GetProperty<float>(ref propertyTable, "ScaleRandMinZ", template.scaleRandMin.z);

		template.scaleRandMax.x =	(float) GetProperty<float>(ref propertyTable, "ScaleRandMaxX", template.scaleRandMax.x);
		template.scaleRandMax.y =	(float) GetProperty<float>(ref propertyTable, "ScaleRandMaxY", template.scaleRandMax.y);
		template.scaleRandMax.z =	(float) GetProperty<float>(ref propertyTable, "ScaleRandMaxZ", template.scaleRandMax.z);

		template.scaleRandMinUniform = (float) GetProperty<float>(ref propertyTable, "ScaleRandMinUniform", template.scaleRandMinUniform);
		template.scaleRandMaxUniform = (float) GetProperty<float>(ref propertyTable, "ScaleRandMaxUniform", template.scaleRandMaxUniform);

		template.scaleUniform = 	(bool) GetProperty<bool>(ref propertyTable, "ScaleUniform", template.scaleUniform);
		#endregion

		#region Sorting Vars
		//Selection
		template.paintToSelection = (bool) GetProperty<bool>(ref propertyTable, "PaintToSelection", template.paintToSelection);

		//Layers
		template.paintToLayer =		(bool) GetProperty<bool>(ref propertyTable, "PaintToLayer", template.paintToLayer);

		template.layerIndex =		(int) GetProperty<int>(ref propertyTable, "LayerIndex", template.layerIndex);

		template.groupObjects =		(bool) GetProperty<bool>(ref propertyTable, "GroupObjects", template.groupObjects);

		//template.groupIndex =		(int) GetProperty<int>(ref propertyTable,"GroupIndex",template.groupIndex);
		template.groupName = 		(string) GetProperty<string>(ref propertyTable, "GroupName", template.groupName);
		#endregion

		#region Eraser Vars
		template.eraseByGroup =		(bool) GetProperty<bool>(ref propertyTable, "EraseByGroup", template.eraseByGroup);
		template.eraseBySelected =	(bool) GetProperty<bool>(ref propertyTable, "EraseBySelected", template.eraseBySelected);
		#endregion

		#region Repopulate the Prefab List
		string prefabGroupString =	(string) GetProperty<string>(ref propertyTable, "PrefabGUIDList", string.Empty);
		qb_PrefabObject[] prefabGroup = new qb_PrefabObject[0];

		string[] prefabStringList = new string[0];
		List<UnityEngine.Object> newPrefabs = new List<UnityEngine.Object>();

		if (prefabGroupString != string.Empty)
		{
			//first clear out any items that are in the prefab list now
			prefabGroup = new qb_PrefabObject[0];
			//then retreive and split the saved prefab guids into a list
			prefabStringList = prefabGroupString.Split('/');//string;

		}

		foreach (string prefabString in prefabStringList)
		{

			if (prefabString == string.Empty)
				continue;

			int splitIndex = prefabString.IndexOf("-");
			string GUIDstring = string.Empty;
			string weightString = string.Empty;

			if (prefabString.Contains("-"))
			{
				GUIDstring = prefabString.Substring(0, splitIndex);
				weightString = prefabString.Substring(splitIndex + 1);
			}

			else
			{
				GUIDstring = prefabString;
			}

			if (GUIDstring == string.Empty)
				continue;

			float itemWeight = 1f;
			if (weightString != null && weightString != string.Empty)
				itemWeight = System.Convert.ToSingle(weightString);

			string assetPath = AssetDatabase.GUIDToAssetPath(GUIDstring);
			Object item = AssetDatabase.LoadAssetAtPath(assetPath, typeof(Object));

			if (item != null)
			{
				newPrefabs.Add(item);

				ArrayUtility.Add(ref prefabGroup, new qb_PrefabObject(item, itemWeight));
			}
		}
		/*
		if(newPrefabs.Count > 0)
		{
			foreach(UnityEngine.Object newPrefab in newPrefabs)
			{
				ArrayUtility.Add(ref prefabGroup,new qb_PrefabObject(newPrefab,1f));
			}
		}
		*/
		template.prefabGroup = prefabGroup;
		#endregion

		template.live = true;

		return template;

	}
	//A property getter to safeguard loading of obsolete files - returns default value if key is not present
	public static T GetProperty<T>(ref Hashtable propertyTable, string propertyKey, T defaultValue)
	{
		T result;

		if (propertyTable.ContainsKey(propertyKey))
			result = (T) propertyTable[propertyKey];

		else
			result = defaultValue;

		return result;

	}

	public static qb_TemplateSignature[] GetTemplateFileSignatures(string directory)
	{
		string[] directories = GetTemplateFileDirectories(directory + "/Templates/");

		qb_TemplateSignature[] signatures = new qb_TemplateSignature[directories.Length];

		for (int i = 0; i < signatures.Length; i++)
		{
			qb_TemplateSignature signature = LoadTemplateSignature(directories[i]);

			signatures[i] = signature;
		}

		return signatures;
	}

	static qb_Template[] GetSavedBrushes(string directory)
	{
		string[] directories = GetTemplateFileDirectories(directory);
		qb_Template[] brushes = new qb_Template[directories.Length];

		for (int i = 0; i < directories.Length; i++)
		{
			qb_Template brush = LoadFromDisk(directories[i]);
			brushes[i] = brush;
		}

		return brushes;
	}

	//Get brush files saved in the qb directory
	static string[] GetTemplateFileDirectories(string directory)
	{
		if (!Directory.Exists(directory))
			Directory.CreateDirectory(directory);

		string[] directories = Directory.GetFiles(directory, "*.qbt");

		return directories;
	}

	public static qb_Template LoadFromEditorPrefs(int slotIndex)
	{
		string prefix = "qbSettings_" + slotIndex.ToString() + "-";

		if (!EditorPrefs.HasKey(prefix))
			return null;

		qb_Template template = new qb_Template();

		template.brushName =			EditorPrefs.GetString(prefix + "BrushName", template.brushName);
		template.lastKnownAs =			EditorPrefs.GetString(prefix + "LastKnownAs", template.lastKnownAs);
		#region Brush Settings Vars
		template.brushRadius =			EditorPrefs.GetFloat(prefix + "BrushRadius", template.brushRadius); //(float) propertyTable["BrushRadius"];
		template.brushRadiusMin =		EditorPrefs.GetFloat(prefix + "BrushRadiusMin", template.brushRadiusMin); //(float) propertyTable["BrushRadiusMin"];
		template.brushRadiusMax =		EditorPrefs.GetFloat(prefix + "BrushRadiusMax", template.brushRadiusMax); //(float) propertyTable["BrushRadiusMax"];
		template.brushSpacing =			EditorPrefs.GetFloat(prefix + "BrushSpacing", template.brushSpacing); //(float) propertyTable["BrushSpacing"];
		template.brushSpacingMin =		EditorPrefs.GetFloat(prefix + "BrushSpacingMin", template.brushSpacingMin); //(float) propertyTable["BrushSpacingMin"];
		template.brushSpacingMax =		EditorPrefs.GetFloat(prefix + "BrushSpacingMax", template.brushSpacingMax); //(float) propertyTable["BrushSpacingMax"];
		template.scatterRadius =		EditorPrefs.GetFloat(prefix + "ScatterRadius", template.scatterRadius); //(float) propertyTable["ScatterRadius"];
		#endregion

		#region Rotation Settings Vars

		template.alignToNormal =		EditorPrefs.GetBool(prefix + "AlignToNormal", template.alignToNormal); //(bool) propertyTable["AlignToNormal"];
		template.flipNormalAlign =		EditorPrefs.GetBool(prefix + "FlipNormalAlign", template.flipNormalAlign); //(bool) propertyTable["FlipNormalAlign"];
		template.alignToStroke =		EditorPrefs.GetBool(prefix + "AlignToStroke", template.alignToStroke); //(bool) propertyTable["AlignToStroke"];
		template.flipStrokeAlign =		EditorPrefs.GetBool(prefix + "FlipStrokeAlign", template.flipStrokeAlign); //(bool) propertyTable["FlipStrokeAlign"];
		template.rotationRangeMin.x =	EditorPrefs.GetFloat(prefix + "RotationRangeMinX", template.rotationRangeMin.x); //(float) propertyTable["RotationRangeMinX"];
		template.rotationRangeMin.y =	EditorPrefs.GetFloat(prefix + "RotationRangeMinY", template.rotationRangeMin.y); //(float) propertyTable["RotationRangeMinY"];
		template.rotationRangeMin.z =	EditorPrefs.GetFloat(prefix + "RotationRangeMinZ", template.rotationRangeMin.z); //(float) propertyTable["RotationRangeMinZ"];
		template.rotationRangeMax.x =	EditorPrefs.GetFloat(prefix + "RotationRangeMaxX", template.rotationRangeMax.x); //(float) propertyTable["RotationRangeMaxX"];
		template.rotationRangeMax.y =	EditorPrefs.GetFloat(prefix + "RotationRangeMaxY", template.rotationRangeMax.y); //(float) propertyTable["RotationRangeMaxY"];
		template.rotationRangeMax.z =	EditorPrefs.GetFloat(prefix + "RotationRangeMaxZ", template.rotationRangeMax.z); //(float) propertyTable["RotationRangeMaxZ"];
		#endregion

		#region Position Settings Vars
		template.positionOffset.x =		EditorPrefs.GetFloat(prefix + "PositionOffsetX", template.positionOffset.x); //(float) propertyTable["PositionOffsetX"];
		template.positionOffset.y =		EditorPrefs.GetFloat(prefix + "PositionOffsetY", template.positionOffset.y); //(float) propertyTable["PositionOffsetY"];
		template.positionOffset.z =		EditorPrefs.GetFloat(prefix + "PositionOffsetZ", template.positionOffset.z); //(float) propertyTable["PositionOffsetZ"];
		#endregion

		#region Scale Settings Vars
		template.scaleAbsolute =		EditorPrefs.GetBool(prefix + "ScaleAbsolute", template.scaleAbsolute);

		//The minimum and maximum possible scale
		template.scaleMin =				EditorPrefs.GetFloat(prefix + "ScaleMin", template.scaleMin); //(float) propertyTable["ScaleMin"];
		template.scaleMax =				EditorPrefs.GetFloat(prefix + "ScaleMax", template.scaleMax); //(float) propertyTable["ScaleMax"];
		//The minimum and maximum current scale range setting
		template.scaleRandMin.x =		EditorPrefs.GetFloat(prefix + "ScaleRandMinX", template.scaleRandMin.x); //(float) propertyTable["ScaleRandMinX"];
		template.scaleRandMin.y =		EditorPrefs.GetFloat(prefix + "ScaleRandMinY", template.scaleRandMin.y); //(float) propertyTable["ScaleRandMinY"];
		template.scaleRandMin.z =		EditorPrefs.GetFloat(prefix + "ScaleRandMinZ", template.scaleRandMin.z); //(float) propertyTable["ScaleRandMinZ"];
		template.scaleRandMax.x =		EditorPrefs.GetFloat(prefix + "ScaleRandMaxX", template.scaleRandMax.x); //(float) propertyTable["ScaleRandMaxX"];
		template.scaleRandMax.y =		EditorPrefs.GetFloat(prefix + "ScaleRandMaxY", template.scaleRandMax.y); //(float) propertyTable["ScaleRandMaxY"];
		template.scaleRandMax.z =		EditorPrefs.GetFloat(prefix + "ScaleRandMaxZ", template.scaleRandMax.z); //(float) propertyTable["ScaleRandMaxZ"];
		template.scaleRandMinUniform =	EditorPrefs.GetFloat(prefix + "ScaleRandMinUniform", template.scaleRandMinUniform); //(float) propertyTable["ScaleRandMinUniform"];
		template.scaleRandMaxUniform =	EditorPrefs.GetFloat(prefix + "ScaleRandMaxUniform", template.scaleRandMaxUniform); //(float) propertyTable["ScaleRandMaxUniform"];
		template.scaleUniform = 		EditorPrefs.GetBool(prefix + "ScaleUniform", template.scaleUniform); //(bool) propertyTable["ScaleUniform"];
		#endregion

		#region Sorting Vars
		//Selection
		template.paintToSelection =		EditorPrefs.GetBool(prefix + "PaintToSelection", template.paintToSelection); //(bool) propertyTable["PaintToSelection"];
		//Layers
		template.paintToLayer =			EditorPrefs.GetBool(prefix + "PaintToLayer", template.paintToLayer); //(bool) propertyTable["PaintToLayer"];
		template.layerIndex =			EditorPrefs.GetInt(prefix + "LayerIndex", template.layerIndex); //(int) propertyTable["LayerIndex"];
		template.groupObjects =			EditorPrefs.GetBool(prefix + "GroupObjects", template.groupObjects);
		//template.groupIndex =			EditorPrefs.GetInt(prefix + "GroupIndex",template.groupIndex);
		template.groupName = 			EditorPrefs.GetString(prefix + "GroupName", template.groupName);
		#endregion

		#region Eraser Vars
		template.eraseByGroup =			EditorPrefs.GetBool(prefix + "EraseByGroup", template.eraseByGroup); //(bool) propertyTable["EraseByGroup"];
		template.eraseBySelected =		EditorPrefs.GetBool(prefix + "EraseBySelected", template.eraseBySelected); //(bool) propertyTable["EraseBySelected"];
		#endregion


		#region Repopulate the Prefab List
		string prefabGroupString =	EditorPrefs.GetString(prefix + "PrefabGUIDList", string.Empty); // (string) propertyTable["PrefabGUIDList"];
		qb_PrefabObject[] prefabGroup = new qb_PrefabObject[0];

		string[] prefabStringList = new string[0];
		List<UnityEngine.Object> newPrefabs = new List<UnityEngine.Object>();

		if (prefabGroupString != string.Empty)
		{
			//first clear out any items that are in the prefab list now
			prefabGroup = new qb_PrefabObject[0];
			//then retreive and split the saved prefab guids into a list
			prefabStringList = prefabGroupString.Split('/');//string;

		}

		foreach (string prefabString in prefabStringList)
		{

			if (prefabString == string.Empty)
				continue;

			int splitIndex = prefabString.IndexOf("-");
			string GUIDstring = string.Empty;
			string weightString = string.Empty;

			if (prefabString.Contains("-"))
			{
				GUIDstring = prefabString.Substring(0, splitIndex);
				weightString = prefabString.Substring(splitIndex + 1);
			}

			else
			{
				GUIDstring = prefabString;
			}

			if (GUIDstring == string.Empty)
				continue;

			float itemWeight = 1f;
			if (weightString != null && weightString != string.Empty)
				itemWeight = System.Convert.ToSingle(weightString);

			string assetPath = AssetDatabase.GUIDToAssetPath(GUIDstring);
			Object item = AssetDatabase.LoadAssetAtPath(assetPath, typeof(Object));

			if (item != null)
			{
				newPrefabs.Add(item);

				ArrayUtility.Add(ref prefabGroup, new qb_PrefabObject(item, itemWeight));
			}
		}
		/*
		if(newPrefabs.Count > 0)
		{
			foreach(UnityEngine.Object newPrefab in newPrefabs)
			{
				ArrayUtility.Add(ref prefabGroup,new qb_PrefabObject(newPrefab,1f));
			}
		}
		*/
		template.prefabGroup = prefabGroup;
		#endregion


		#region Maintenance Vars
		template.dirty = EditorPrefs.GetBool(prefix + "Dirty", template.dirty);
		template.live = true;
		#endregion


		#region LiveVars

		template.active = EditorPrefs.GetBool(prefix + "Active", false);

		template.selectedPrefabIndex = EditorPrefs.GetInt(prefix + "SelectedPrefabIndex", -1);

		#endregion

		return template;
	}

	public static void SaveToEditorPrefs(int slotIndex, qb_Template template)
	{
		string prefix = "qbSettings_" + slotIndex.ToString() + "-";

		EditorPrefs.SetString(prefix, template.brushName);

		EditorPrefs.SetString(prefix + "BrushName", template.brushName);

		EditorPrefs.SetString(prefix + "LastKnownAs", template.lastKnownAs);

		#region Brush Settings Vars
		EditorPrefs.SetFloat(prefix + "BrushRadius", template.brushRadius);

		EditorPrefs.SetFloat(prefix + "BrushRadiusMin", template.brushRadiusMin);

		EditorPrefs.SetFloat(prefix + "BrushRadiusMax", template.brushRadiusMax);

		EditorPrefs.SetFloat(prefix + "BrushSpacing", template.brushSpacing);

		EditorPrefs.SetFloat(prefix + "BrushSpacingMin", template.brushSpacingMin);

		EditorPrefs.SetFloat(prefix + "BrushSpacingMax", template.brushSpacingMax);

		EditorPrefs.SetFloat(prefix + "ScatterRadius", template.scatterRadius);
		#endregion

		#region Rotation Settings Vars
		EditorPrefs.SetBool(prefix + "AlignToNormal", template.alignToNormal);

		EditorPrefs.SetBool(prefix + "FlipNormalAlign", template.flipNormalAlign);

		EditorPrefs.SetBool(prefix + "AlignToStroke", template.alignToStroke);

		EditorPrefs.SetBool(prefix + "FlipStrokeAlign", template.flipStrokeAlign);

		EditorPrefs.SetFloat(prefix + "RotationRangeMinX", template.rotationRangeMin.x);
		EditorPrefs.SetFloat(prefix + "RotationRangeMinY", template.rotationRangeMin.y);
		EditorPrefs.SetFloat(prefix + "RotationRangeMinZ", template.rotationRangeMin.z);

		EditorPrefs.SetFloat(prefix + "RotationRangeMaxX", template.rotationRangeMax.x);
		EditorPrefs.SetFloat(prefix + "RotationRangeMaxY", template.rotationRangeMax.y);
		EditorPrefs.SetFloat(prefix + "RotationRangeMaxZ", template.rotationRangeMax.z);
		#endregion

		#region Position Settings Vars
		EditorPrefs.SetFloat(prefix + "PositionOffsetX", template.positionOffset.x);
		EditorPrefs.SetFloat(prefix + "PositionOffsetY", template.positionOffset.y);
		EditorPrefs.SetFloat(prefix + "PositionOffsetZ", template.positionOffset.z);
		#endregion

		#region Scale Settings Vars
		EditorPrefs.SetBool(prefix + "ScaleAbsolute", template.scaleAbsolute);

		//The minimum and maximum possible scale
		EditorPrefs.SetFloat(prefix + "ScaleMin", template.scaleMin);

		EditorPrefs.SetFloat(prefix + "ScaleMax", template.scaleMax);

		//The minimum and maximum current scale range setting
		EditorPrefs.SetFloat(prefix + "ScaleRandMinX", template.scaleRandMin.x);
		EditorPrefs.SetFloat(prefix + "ScaleRandMinY", template.scaleRandMin.y);
		EditorPrefs.SetFloat(prefix + "ScaleRandMinZ", template.scaleRandMin.z);

		EditorPrefs.SetFloat(prefix + "ScaleRandMaxX", template.scaleRandMax.x);
		EditorPrefs.SetFloat(prefix + "ScaleRandMaxY", template.scaleRandMax.y);
		EditorPrefs.SetFloat(prefix + "ScaleRandMaxZ", template.scaleRandMax.z);

		EditorPrefs.SetFloat(prefix + "ScaleRandMinUniform", template.scaleRandMinUniform);

		EditorPrefs.SetFloat(prefix + "ScaleRandMaxUniform", template.scaleRandMaxUniform);

		EditorPrefs.SetBool(prefix + "ScaleUniform", template.scaleUniform);
		#endregion

		#region Sorting Vars
		//Selection
		EditorPrefs.SetBool(prefix + "PaintToSelection", template.paintToSelection);

		//Layers
		EditorPrefs.SetBool(prefix + "PaintToLayer", template.paintToLayer);

		EditorPrefs.SetInt(prefix + "LayerIndex", template.layerIndex);

		EditorPrefs.SetBool(prefix + "GroupObjects", template.groupObjects);

		//EditorPrefs.SetInt(prefix + "GroupIndex",template.groupIndex);
		EditorPrefs.SetString(prefix + "GroupName", template.groupName);
		#endregion

		#region Eraser Vars
		EditorPrefs.SetBool(prefix + "EraseByGroup", template.eraseByGroup);
		EditorPrefs.SetBool(prefix + "EraseBySelected", template.eraseBySelected);
		#endregion

		//New Prefab Save
		string prefabGroupString = string.Empty;

		for (int i = 0; i < template.prefabGroup.Length; i ++)
		{
			prefabGroupString += "/" + AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(template.prefabGroup[i].prefab)) + "-" + template.prefabGroup[i].weight.ToString();
			//prefabGroupString += "/" + AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(template.prefabGroup[i].prefab)) + "-" + template.prefabGroup[i].weight.ToString() + ">" + template.prefabGroup[i];

		}

		EditorPrefs.SetString(prefix + "PrefabGUIDList", prefabGroupString);

		#region Maintenance Vars
		EditorPrefs.SetBool(prefix + "Dirty", template.dirty);
		#endregion

		//return propertyTable;
		#region LiveVars

		EditorPrefs.SetBool(prefix + "Active", template.active);

		EditorPrefs.SetInt(prefix + "SelectedPrefabIndex", template.selectedPrefabIndex);

		#endregion
	}

	public static void ClearSlotFromEditorPrefs(int slotNum) //if slot is dirty, qb_painter should prompt to save before calling this funnction
	{
		string prefix = "qbSettings_" + slotNum.ToString() + "-";
		//For starters, we can just clear the prefix - loader checks for the slot key, so if that's not present the rest won't be loaded up
		//A full clear is probably not necessary or useful - just extra operations to slow things down,1 marginally
		EditorPrefs.DeleteKey(prefix);
	}

}

