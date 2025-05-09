﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class UIManager : Singleton<UIManager>, IInitializable
{
    public bool IsInitialized { get; private set; }

    private const string PANEL_PATH = "Prefabs/UI/Panels/";
    private List<Panel> panelPrefabs = new List<Panel>();
    private List<Panel> panels = new List<Panel>();
    public List<Panel> Panels => panels;

    [Header("UI Panels")]
    public Canvas mainCanvas;

    public void Initialize()
    {
        try
        {
            LoadResources();
            IsInitialized = true;
        }
        catch (Exception e)
        {
            Logger.LogError(typeof(UIManager), $"Error initializing UIManager: {e.Message}");
            IsInitialized = false;
        }
    }

    private void LoadResources()
    {
        panelPrefabs = Resources.LoadAll<Panel>(PANEL_PATH).ToList();
    }

    public Panel OpenPanel(PanelType panelType)
    {
        Panel panel = Panels.Find(p => p.PanelType == panelType);
        if (panel != null)
        {
            panel.Open();
            return panel;
        }
        else
        {
            Panel prefab = panelPrefabs.Find(p => p.PanelType == panelType);
            if (prefab != null)
            {
                Panel instance = Instantiate(prefab, mainCanvas.transform);
                instance.name = prefab.name;
                Panels.Add(instance);
                instance.Open();
                return instance;
            }
        }
        Logger.LogWarning(typeof(UIManager), $"Panel {panelType} not found");
        return null;
    }

    public void ClosePanel(PanelType panelType, bool objActive = true)
    {
        Panel panel = Panels.Find(p => p.PanelType == panelType);
        if (panel == null)
        {
            Logger.LogError(typeof(UIManager), $"PanelType {panelType} not found");
            return;
        }
        panel.Close(objActive);
    }

    public Panel GetPanel(PanelType panelType)
    {
        Panel panel = Panels.Find(p => p.PanelType == panelType);
        if (panel == null)
        {
            Panel prefab = panelPrefabs.Find(p => p.PanelType == panelType);
            if (prefab != null)
            {
                panel = Instantiate(prefab, mainCanvas.transform);
                Panels.Add(panel);
            }
        }
        return panel;
    }

    public void CloseAllPanels()
    {
        foreach (var panel in Panels)
        {
            panel.Close(false);
        }
    }
}
