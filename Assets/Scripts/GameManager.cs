﻿using System.Collections;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SharpConfig; // https://github.com/cemdervis/SharpConfig for documentation

// Same as InputManager's InputKeys struct - just a handy tool for passing around settings info
public struct GameSettings {
    public int resolutionX, resolutionY;
    public bool fullscreen;
    public int fov;
    public float mouseSensitivityX, mouseSensitivityY;
    public float musicVolume, sfxVolume, crowdVolume;
    public int gfxPreset;
    public Color playerColor;
    public string playerName;
}

public class GameManager : MonoBehaviour {

    public static GameManager instance;
    public Canvas canvas; // Ref here so no UI elements have to GameObject.Find it every time

    public MusicManager musicManager;
    public ScreenFader blackFade;

    const string SETTINGS_FILENAME = "settings.cfg";

    public Vector2 mouseSensitivity;
    public Color playerColor;
    public string playerName;

    Configuration config;

    void Awake() {
        // Singleton stuff, needs to carry across multiple scenes
        if (instance != null && instance != this)
            Destroy(gameObject);
        instance = this;
        DontDestroyOnLoad(gameObject);
        
        canvas = GameObject.Find("Canvas").GetComponent<Canvas>();
        
        // Read config file -- if none exists, make one
        try {
            config = Configuration.LoadFromFile(SETTINGS_FILENAME);
            // Set settings
            GameSettings s = ConfigurationToGameSettings(config);
            ApplyOptionsSettings(s);
            ApplyPlayerSettings(s);
            // Set controls
            InputKeys k = ConfigurationToInputKeys(config);
            InputManager.SetKeys(k);
        } catch (FileNotFoundException) {
            Debug.Log("No config file '" + SETTINGS_FILENAME + "' found. Making one with defaults!");
            config = MakeDefaultConfig(true);
            config.SaveToFile(SETTINGS_FILENAME);
        }

        canvas.GetComponent<MenuUIManager>().PlayIntro();
    }

    ///////////////////////
    //  CONFIG/SETTINGS  //
    ///////////////////////

    public Configuration GetConfiguration() {
        return config;
    }

    public GameSettings ConfigurationToGameSettings(Configuration cfg) {
        GameSettings settings = new GameSettings {
            resolutionX = cfg["Settings"]["ResolutionX"].IntValue,
            resolutionY = cfg["Settings"]["ResolutionY"].IntValue,
            fullscreen = cfg["Settings"]["Fullscreen"].BoolValue,
            fov = cfg["Settings"]["FOV"].IntValue,
            mouseSensitivityX = cfg["Settings"]["MouseSensitivityX"].FloatValue,
            mouseSensitivityY = cfg["Settings"]["MouseSensitivityY"].FloatValue,
            musicVolume = cfg["Settings"]["MusicVolume"].FloatValue,
            sfxVolume = cfg["Settings"]["SFXVolume"].FloatValue,
            crowdVolume = cfg["Settings"]["CrowdVolume"].FloatValue,
            gfxPreset = cfg["Settings"]["GFXPreset"].IntValue
        };
        float[] c = config["Settings"]["PlayerColor"].FloatValueArray;
        settings.playerColor = new Color(c[0], c[1], c[2]);
        settings.playerName = config["Settings"]["PlayerName"].StringValue;

        return settings;
    }

    public InputKeys ConfigurationToInputKeys(Configuration cfg) {
        InputKeys keys = new InputKeys {
            left = (KeyCode)cfg["Controls"]["Left"].IntValueArray[0],
            leftAlt = (KeyCode)cfg["Controls"]["Left"].IntValueArray[1],
            right = (KeyCode)cfg["Controls"]["Right"].IntValueArray[0],
            rightAlt = (KeyCode)cfg["Controls"]["Right"].IntValueArray[1],
            up = (KeyCode)cfg["Controls"]["Up"].IntValueArray[0],
            upAlt = (KeyCode)cfg["Controls"]["Up"].IntValueArray[1],
            down = (KeyCode)cfg["Controls"]["Down"].IntValueArray[0],
            downAlt = (KeyCode)cfg["Controls"]["Down"].IntValueArray[1],
            jump = (KeyCode)cfg["Controls"]["Jump"].IntValueArray[0],
            jumpAlt = (KeyCode)cfg["Controls"]["Jump"].IntValueArray[1],
            fire1 = (KeyCode)cfg["Controls"]["Fire1"].IntValueArray[0],
            fire1Alt = (KeyCode)cfg["Controls"]["Fire1"].IntValueArray[1],
            fire2 = (KeyCode)cfg["Controls"]["Fire2"].IntValueArray[0],
            fire2Alt = (KeyCode)cfg["Controls"]["Fire2"].IntValueArray[1],
            joystick = cfg["Controls"]["UseJoystick"].BoolValue
        };

        return keys;
    }

    public void SetOptionSettings(GameSettings settings) {
        ApplyOptionsSettings(settings);

        // Just take the settings we have now, so they don't get modified
        float[] c = config["Settings"]["PlayerColor"].FloatValueArray;
        settings.playerColor = new Color(c[0], c[1], c[2]);
        settings.playerName = config["Settings"]["PlayerName"].StringValue;

        SaveGameSettingsToConfig(config, settings);

        config.SaveToFile(SETTINGS_FILENAME);
    }

    void ApplyOptionsSettings(GameSettings settings) {
        Screen.SetResolution(settings.resolutionX, settings.resolutionY, settings.fullscreen);
        QualitySettings.SetQualityLevel(settings.gfxPreset, true);
        Camera.main.fieldOfView = settings.fov;
        mouseSensitivity.x = settings.mouseSensitivityX;
        mouseSensitivity.y = settings.mouseSensitivityY;
        musicManager.musicVolume = settings.musicVolume;
        // TODO SFX AND CROWD VOLUMES
    }

    public void SetOptionPlayer(GameSettings settings) {
        ApplyPlayerSettings(settings);

        // Write to config
        config["Settings"]["PlayerColor"].FloatValueArray = new float[] { settings.playerColor.r, settings.playerColor.g, settings.playerColor.b };
        config["Settings"]["PlayerName"].StringValue = settings.playerName;

        config.SaveToFile(SETTINGS_FILENAME);
    }

    void ApplyPlayerSettings(GameSettings settings) {
        playerColor = settings.playerColor;
        playerName = settings.playerName;
    }

    public KeyValuePair<int, int> GetCustomResolution() {
        return new KeyValuePair<int, int>(config["Settings"]["ResolutionX"].IntValue, config["Settings"]["ResolutionY"].IntValue);
    }

    public void SaveControlsToConfig(Configuration cfg, InputKeys keys) {
        cfg["Controls"]["Left"].IntValueArray = new int[] { (int) keys.left, (int) keys.leftAlt };
        cfg["Controls"]["Right"].IntValueArray = new int[] { (int) keys.right, (int) keys.rightAlt };
        cfg["Controls"]["Up"].IntValueArray = new int[] { (int) keys.up, (int) keys.upAlt };
        cfg["Controls"]["Down"].IntValueArray = new int[] { (int) keys.down, (int) keys.downAlt };
        cfg["Controls"]["Jump"].IntValueArray = new int[] { (int) keys.jump, (int) keys.jumpAlt };
        cfg["Controls"]["Fire1"].IntValueArray = new int[] { (int) keys.fire1, (int) keys.fire1Alt };
        cfg["Controls"]["Fire2"].IntValueArray = new int[] { (int) keys.fire2, (int) keys.fire2Alt };
        cfg["Controls"]["UseJoystick"].BoolValue = keys.joystick;
    }

    public void SaveControlsToConfig(InputKeys keys) {
        SaveControlsToConfig(config, keys);
        config.SaveToFile(SETTINGS_FILENAME);
    }

    public GameSettings GetDefaultSettings() {
        GameSettings settings = new GameSettings() {
            resolutionX = Screen.currentResolution.width,
            resolutionY = Screen.currentResolution.height,
            fullscreen = true,
            fov = 60,
            mouseSensitivityX = 1f,
            mouseSensitivityY = 1f,
            musicVolume = 0.5f,
            sfxVolume = 0.7f,
            crowdVolume = 0.6f,
            gfxPreset = QualitySettings.names.Length/2, // Unity has no "recommended settings" functionality, so just go for the median option
            playerColor = Color.white,
            playerName = "Contestant"
        };
        return settings;
    }

    Configuration MakeDefaultConfig(bool resetControls) {
        Configuration cfg = new Configuration();

        GameSettings settings = GetDefaultSettings();

        SaveGameSettingsToConfig(cfg, settings);

        if (resetControls) InputManager.SetKeys(InputManager.GetDefaultKeys());

        cfg["Controls"].Comment = "Controls value corresponds to Unity's KeyCode enum value. {Main Key, Alt Key}";
        SaveControlsToConfig(cfg, InputManager.GetDefaultKeys());

        return cfg;
    }

    void SaveGameSettingsToConfig(Configuration cfg, GameSettings settings) {
        cfg["Settings"]["ResolutionX"].IntValue = settings.resolutionX;
        cfg["Settings"]["ResolutionY"].IntValue = settings.resolutionY;
        cfg["Settings"]["Fullscreen"].BoolValue = settings.fullscreen;
        cfg["Settings"]["FOV"].IntValue = settings.fov;
        cfg["Settings"]["MouseSensitivityX"].FloatValue = settings.mouseSensitivityX;
        cfg["Settings"]["MouseSensitivityY"].FloatValue = settings.mouseSensitivityY;
        cfg["Settings"]["MusicVolume"].FloatValue = settings.musicVolume;
        cfg["Settings"]["SFXVolume"].FloatValue = settings.sfxVolume;
        cfg["Settings"]["CrowdVolume"].FloatValue = settings.crowdVolume;
        cfg["Settings"]["GFXPreset"].IntValue = settings.gfxPreset;
        cfg["Settings"]["PlayerColor"].FloatValueArray = new float[] { settings.playerColor.r, settings.playerColor.g, settings.playerColor.b };
        cfg["Settings"]["PlayerName"].StringValue = settings.playerName;
    }
}
