using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Static debug logger for VR that can be accessed from any module.
/// Usage: DebugVR.Log("Your message here");
/// </summary>
public static class DebugVR
{
    // Queue to store recent debug messages
    private static readonly Queue<string> _messageQueue = new Queue<string>();
    
    // Maximum number of messages to keep in the queue
    private static int _maxMessages = 20;
    
    // Reference to the UI drawer component
    private static DebugUIDrawer _drawer;
    
    // Flag to prevent multiple initialization attempts
    private static bool _isInitializing = false;
    
    /// <summary>
    /// Sets the maximum number of messages to display.
    /// </summary>
    public static void SetMaxMessages(int count)
    {
        if (count < 1) count = 1;
        _maxMessages = count;
        
        // Trim existing queue if needed
        while (_messageQueue.Count > _maxMessages)
        {
            _messageQueue.Dequeue();
        }
    }
    
    /// <summary>
    /// Logs a message to the debug display.
    /// Thread-safe for main thread usage (Unity's main thread).
    /// </summary>
    public static void Log(string message)
    {
        // Add message to queue
        lock (_messageQueue)
        {
            _messageQueue.Enqueue(message);
            
            // Remove oldest messages if we exceed limit
            while (_messageQueue.Count > _maxMessages)
            {
                _messageQueue.Dequeue();
            }
        }
        
        // Initialize drawer if needed (lazy initialization)
        if (_drawer == null && !_isInitializing)
        {
            _isInitializing = true;
            InitializeDrawer();
        }
    }
    
    /// <summary>
    /// Clears all debug messages.
    /// </summary>
    public static void Clear()
    {
        lock (_messageQueue)
        {
            _messageQueue.Clear();
        }
    }
    
    /// <summary>
    /// Initializes the debug UI drawer component.
    /// </summary>
    private static void InitializeDrawer()
    {
        // Check if already initialized by another thread
        if (_drawer != null)
        {
            _isInitializing = false;
            return;
        }
        
        // Look for existing drawer in scene
        _drawer = Object.FindObjectOfType<DebugUIDrawer>();
        
        // If not found, create one
        if (_drawer == null)
        {
            // Create a new GameObject to hold our drawer
            GameObject go = new GameObject("[DebugVR System]");
            
            // Make it persistent across scene loads
            Object.DontDestroyOnLoad(go);
            
            // Add our drawer component
            _drawer = go.AddComponent<DebugUIDrawer>();
            
            // Hide it in hierarchy by default (optional)
            go.hideFlags = HideFlags.HideAndDontSave;
        }
        
        _isInitializing = false;
    }
    
    /// <summary>
    /// Component that draws the debug messages using OnGUI.
    /// </summary>
    private class DebugUIDrawer : MonoBehaviour
    {
        // GUI style for the debug text
        private GUIStyle _style;
        
        // Window rectangle
        private Rect _windowRect = new Rect(10, 10, 400, 300);
        
        // Background color (semi-transparent black)
        private Color _backgroundColor = new Color(0f, 0f, 0f, 0.7f);
        
        private void Awake()
        {
            // Initialize GUI style
            _style = new GUIStyle(GUI.skin.label);
            _style.fontSize = 18;
            _style.normal.textColor = Color.yellow;
            _style.wordWrap = true;
            _style.richText = true; // Allow <color> tags etc.
        }
        
        private void OnGUI()
        {
            // Draw background
            GUI.Box(_windowRect, GUIContent.none);
            
            // Begin group for clipping
            GUI.BeginGroup(_windowRect);
            
            // Draw messages
            float yOffset = 5f;
            float lineHeight = _style.lineHeight + 2f;
            
            lock (_messageQueue)
            {
                // We need to copy the queue to iterate since we can't modify during enumeration
                var messages = new List<string>(_messageQueue);
                
                // Display messages from newest to oldest (or reverse as preferred)
                for (int i = messages.Count - 1; i >= 0; i--)
                {
                    string message = messages[i];
                    GUI.Label(new Rect(5f, yOffset, _windowRect.width - 10f, lineHeight), message, _style);
                    yOffset += lineHeight;
                    
                    // Stop if we've drawn enough lines to fill the window
                    if (yOffset > _windowRect.height - lineHeight)
                        break;
                }
            }
            
            // End group
            GUI.EndGroup();
        }
    }
}