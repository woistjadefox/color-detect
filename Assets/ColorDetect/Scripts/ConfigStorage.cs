using UnityEngine;
using System;
using System.IO;
using System.Xml;
using System.Xml.XPath;
using System.Collections;
using System.Collections.Generic;

public class PresetFile {
	public string path = "";
	public string name = "";
	public PresetFile(string inPath, string inName) {
		path = inPath;
		name = inName;
	}
}

public class PresetsConfigStorage {

	private List<PresetFile> availablePresets = new List<PresetFile>();
	
	/// <summary>
	/// The reference to the XML document.
	/// </summary>
	private XmlDocument doc;
	/// <summary>
	/// The preset folder path.
	/// </summary>
	private string presetPath;
	
	/// <summary>
	/// Is 'true' if there was an error
	/// </summary>
	private bool error = false;
	
	public PresetsConfigStorage() {
		//set path
		presetPath = Application.dataPath + "/ColorDetect/presets/";
		
		//read data from files
		this.loadPresets();
	}
	
	public int getPresetCount() {
		return availablePresets.Count;
	}
	
	public string getNameAtPosition(int pos) {
		return availablePresets[pos].name;
	}
	
	public ThresholdHSVPreset getPresetAtPos(int pos) {
		return this.loadPreset(availablePresets[pos].path);
	}
	
	public void savePreset(ThresholdHSVPreset inPreset) {
		
		//TODO
		
	}
	
	private void loadPresets() {
		//fetch file names
		string[] filePaths = Directory.GetFiles(presetPath, "*.xml");
		
		//check files
		foreach (string file in filePaths) {
			error = false;
			doc = new XmlDocument();
			
			try {
				using (StreamReader sr = new StreamReader(file)) {
					doc.LoadXml(sr.ReadToEnd());
				}
			} catch {
				error = true;
			}
			
			string colorname = "";
			
			if (!error) {
				try {
					//select node
					XmlNode node = doc.SelectSingleNode("/colortracking/preset");
					colorname = node.FirstChild.InnerText;
				
				} catch {
					error = true;
				}
				
				//add to list
				if (!error && colorname != "") {
					availablePresets.Add(new PresetFile(file, colorname));
				}
			}
		}
	}
	
	private ThresholdHSVPreset loadPreset(string inPath) {
		
		//init values
		ThresholdHSVPreset loadedPreset = new ThresholdHSVPreset(new ColorHSV(-1, -1, -1), new ColorHSV(-1, -1, -1));	
		error = false;
		
		//open XML file
		doc = new XmlDocument();
		try {
			using (StreamReader sr = new StreamReader(inPath)) {
				doc.LoadXml(sr.ReadToEnd());
			}
		} catch {
			error = true;
		}
		
		if (!error) {
			
			ColorHSV colorLow = new ColorHSV();
			ColorHSV colorHigh = new ColorHSV();
			XmlNode tempNode;
			
			try {
				//read low bound
				tempNode = doc.SelectSingleNode("/colortracking/values/colorlow");
				tempNode = tempNode.FirstChild;
				colorLow.hue = Convert.ToInt32(tempNode.InnerText);
				tempNode = tempNode.NextSibling;
				colorLow.saturation = Convert.ToInt32(tempNode.InnerText);
				tempNode = tempNode.NextSibling;
				colorLow.value = Convert.ToInt32(tempNode.InnerText);
				
				//read low bound
				tempNode = doc.SelectSingleNode("/colortracking/values/colorhigh");
				tempNode = tempNode.FirstChild;
				colorHigh.hue = Convert.ToInt32(tempNode.InnerText);
				tempNode = tempNode.NextSibling;
				colorHigh.saturation = Convert.ToInt32(tempNode.InnerText);
				tempNode = tempNode.NextSibling;
				colorHigh.value = Convert.ToInt32(tempNode.InnerText);
			} catch {
				error = true;
			}
			
			if (!error) {
				loadedPreset.lowBound = colorLow;
				loadedPreset.highBound = colorHigh;
			}
		}
		
		return loadedPreset;
	}
}
