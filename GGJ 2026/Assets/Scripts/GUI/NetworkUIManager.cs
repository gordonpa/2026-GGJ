using System.Net;
using System.Net.Sockets;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

/// <summary>
/// 网络UI管理器 - 提供Host/Client选择界面和端口/IP配置
/// </summary>
public class NetworkUIManager : MonoBehaviour
{
    [Header("网络设置")]
    [SerializeField] private string serverIP = "127.0.0.1";
    [SerializeField] private ushort serverPort = 7777;
    
    [Header("UI设置")]
    [SerializeField] private bool showNetworkUI = true;
    
    [Header("字体和样式设置")]
    [Tooltip("自定义字体（可选，为空则使用默认字体）")]
    [SerializeField] private Font customFont;
    
    [Tooltip("按钮正常状态图片（可选）")]
    [SerializeField] private Texture2D buttonNormalImage;
    
    [Tooltip("按钮悬停状态图片（可选）")]
    [SerializeField] private Texture2D buttonHoverImage;
    
    [Tooltip("按钮按下状态图片（可选）")]
    [SerializeField] private Texture2D buttonActiveImage;
    
    [Header("连接界面设置")]
    [Tooltip("位置百分比 (0-1)，相对于屏幕")]
    [SerializeField] private Vector2 connectionUIPositionPercent = new Vector2(0.01f, 0.01f);
    [Tooltip("大小百分比 (0-1)，相对于屏幕")]
    [SerializeField] private Vector2 connectionUISizePercent = new Vector2(0.25f, 0.4f);
    
    [Header("状态界面设置")]
    [Tooltip("位置百分比 (0-1)，相对于屏幕")]
    [SerializeField] private Vector2 statusUIPositionPercent = new Vector2(0.01f, 0.01f);
    [Tooltip("大小百分比 (0-1)，相对于屏幕")]
    [SerializeField] private Vector2 statusUISizePercent = new Vector2(0.25f, 0.2f);
    
    private string ipInput = "127.0.0.1";
    private string portInput = "7777";
    private string cachedLocalLanIP;
    
    // GUI样式缓存
    private GUIStyle buttonStyle;
    private GUIStyle labelStyle;
    private GUIStyle boxStyle;
    private bool stylesInitialized = false;
    
    private void Start()
    {
        ipInput = serverIP;
        portInput = serverPort.ToString();
    }
    
    /// <summary>获取本机局域网 IP（如 192.168.1.100），用于 Host 时告知对方连接地址。</summary>
    private string GetLocalLanIP()
    {
        if (!string.IsNullOrEmpty(cachedLocalLanIP)) return cachedLocalLanIP;
        try
        {
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                var endPoint = socket.LocalEndPoint as IPEndPoint;
                if (endPoint != null)
                {
                    cachedLocalLanIP = endPoint.Address.ToString();
                    return cachedLocalLanIP;
                }
            }
        }
        catch { }
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                {
                    cachedLocalLanIP = ip.ToString();
                    return cachedLocalLanIP;
                }
            }
        }
        catch { }
        cachedLocalLanIP = "127.0.0.1";
        return cachedLocalLanIP;
    }
    
    /// <summary>
    /// 初始化GUI样式
    /// </summary>
    private void InitializeStyles()
    {
        if (stylesInitialized) return;
        
        // 创建按钮样式
        buttonStyle = new GUIStyle(GUI.skin.button);
        if (buttonNormalImage != null)
        {
            buttonStyle.normal.background = buttonNormalImage;
        }
        if (buttonHoverImage != null)
        {
            buttonStyle.hover.background = buttonHoverImage;
        }
        if (buttonActiveImage != null)
        {
            buttonStyle.active.background = buttonActiveImage;
        }
        
        // 创建标签样式
        labelStyle = new GUIStyle(GUI.skin.label);
        if (customFont != null)
        {
            labelStyle.font = customFont;
            buttonStyle.font = customFont;
        }
        
        // 创建Box样式
        boxStyle = new GUIStyle(GUI.skin.box);
        if (customFont != null)
        {
            boxStyle.font = customFont;
        }
        
        stylesInitialized = true;
    }
    
    private void OnGUI()
    {
        if (!showNetworkUI) return;
        
        // 初始化样式
        InitializeStyles();
        
        var networkManager = NetworkManager.Singleton;
        
        // 检查 NetworkManager 是否存在
        if (networkManager == null)
        {
            // NetworkManager 尚未初始化，显示错误提示
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;
            float posX = connectionUIPositionPercent.x * screenWidth;
            float posY = connectionUIPositionPercent.y * screenHeight;
            float width = connectionUISizePercent.x * screenWidth;
            float height = connectionUISizePercent.y * screenHeight;
            
            Rect errorRect = new Rect(posX, posY, width, height);
            GUILayout.BeginArea(errorRect);
            GUILayout.Box("网络管理器未找到", boxStyle);
            GUILayout.Space(10);
            GUILayout.Label("请确保场景中存在 NetworkManager 组件", labelStyle);
            GUILayout.EndArea();
            return;
        }
        
        // 如果未连接，显示连接界面
        if (!networkManager.IsClient && !networkManager.IsServer)
        {
            DrawConnectionUI();
        }
        else
        {
            DrawStatusUI();
        }
    }
    
    /// <summary>
    /// 绘制连接界面（Host/Client选择和端口配置）
    /// </summary>
    private void DrawConnectionUI()
    {
        var networkManager = NetworkManager.Singleton;
        if (networkManager == null) return;
        
        // 根据屏幕尺寸计算实际位置和大小
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;
        float posX = connectionUIPositionPercent.x * screenWidth;
        float posY = connectionUIPositionPercent.y * screenHeight;
        float width = connectionUISizePercent.x * screenWidth;
        float height = connectionUISizePercent.y * screenHeight;
        
        Rect connectionRect = new Rect(posX, posY, width, height);
        GUILayout.BeginArea(connectionRect);
        GUILayout.Box("网络连接设置", boxStyle);
        
        GUILayout.Space(10);
        
        // 本机局域网 IP 显示与一键填入
        string localIP = GetLocalLanIP();
        GUILayout.Label($"本机局域网 IP: {localIP}", labelStyle);
        if (GUILayout.Button("服务器 IP 填为本机 IP", buttonStyle, GUILayout.Height(28)))
        {
            ipInput = localIP;
        }
        GUILayout.Space(5);
        
        // IP 地址输入（可手动改）
        GUILayout.Label("服务器 IP 地址:", labelStyle);
        float inputFieldWidth = connectionUISizePercent.x * Screen.width - 20;
        ipInput = GUILayout.TextField(ipInput, GUILayout.Width(inputFieldWidth));
        
        GUILayout.Space(5);
        
        // 端口：支持 4 位如 7777（1-65535）
        GUILayout.Label("端口 (4 位如 7777):", labelStyle);
        portInput = GUILayout.TextField(portInput, GUILayout.Width(inputFieldWidth));
        
        GUILayout.Space(5);
        GUILayout.Label("Host/Server 填本机端口；Client 填对方 IP 和端口", labelStyle);
        GUILayout.Space(5);
        
        // 解析端口
        if (ushort.TryParse(portInput, out ushort port))
        {
            serverPort = port;
        }
        else
        {
            GUILayout.Label("端口格式错误！", boxStyle);
        }
        
        GUILayout.Space(10);
        
        // Host按钮：服务器监听 0.0.0.0 以便其他电脑能连入
        float buttonHeight = 40 * (Screen.height / 1080f); // 基于1080p参考分辨率
        if (GUILayout.Button("启动 Host", buttonStyle, GUILayout.Height(buttonHeight)))
        {
            SetConnectionData(ipInput, serverPort, listenOnAllInterfaces: true);
            networkManager.StartHost();
        }
        
        GUILayout.Space(5);
        
        // Client按钮：连接到输入的 IP:端口
        if (GUILayout.Button("连接 Client", buttonStyle, GUILayout.Height(buttonHeight)))
        {
            SetConnectionData(ipInput, serverPort, listenOnAllInterfaces: false);
            networkManager.StartClient();
        }
        
        GUILayout.Space(5);
        
        // Server按钮：监听所有网卡
        if (GUILayout.Button("启动 Server", buttonStyle, GUILayout.Height(buttonHeight)))
        {
            SetConnectionData(ipInput, serverPort, listenOnAllInterfaces: true);
            networkManager.StartServer();
        }
        
        GUILayout.EndArea();
    }
    
    /// <summary>
    /// 绘制状态界面（显示当前连接状态）
    /// </summary>
    private void DrawStatusUI()
    {
        var networkManager = NetworkManager.Singleton;
        if (networkManager == null) return;
        
        // 根据屏幕尺寸计算实际位置和大小
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;
        float posX = statusUIPositionPercent.x * screenWidth;
        float posY = statusUIPositionPercent.y * screenHeight;
        float width = statusUISizePercent.x * screenWidth;
        float height = statusUISizePercent.y * screenHeight;
        
        Rect statusRect = new Rect(posX, posY, width, height);
        GUILayout.BeginArea(statusRect);
        GUILayout.Box("网络状态", boxStyle);
        
        GUILayout.Space(10);
        
        // 显示当前模式
        string mode = networkManager.IsHost ? "Host" : 
                      networkManager.IsServer ? "Server" : "Client";
        GUILayout.Label($"模式: {mode}", labelStyle);
        
        // 显示连接信息
        if (networkManager.IsClient || networkManager.IsHost)
        {
            GUILayout.Label($"服务器: {ipInput}:{serverPort}", labelStyle);
        }
        
        GUILayout.Space(10);
        
        // 断开连接按钮（高度也根据屏幕缩放）
        float statusButtonHeight = 30 * (Screen.height / 1080f); // 基于1080p参考分辨率
        if (GUILayout.Button("断开连接", buttonStyle, GUILayout.Height(statusButtonHeight)))
        {
            if (networkManager.IsHost)
            {
                networkManager.Shutdown();
            }
            else if (networkManager.IsServer)
            {
                networkManager.Shutdown();
            }
            else if (networkManager.IsClient)
            {
                networkManager.Shutdown();
            }
        }
        
        GUILayout.EndArea();
    }
    
    /// <summary>
    /// 设置连接数据（IP 和端口）。Host/Server 时监听 0.0.0.0 以便其他电脑能连入。
    /// </summary>
    /// <param name="ip">客户端：要连接的服务器 IP；Host/Server：本机显示用，实际监听由 listenOnAllInterfaces 决定</param>
    /// <param name="port">端口号</param>
    /// <param name="listenOnAllInterfaces">为 true 时服务器监听 0.0.0.0（所有网卡），局域网/另一台电脑可连接</param>
    private void SetConnectionData(string ip, ushort port, bool listenOnAllInterfaces = false)
    {
        var networkManager = NetworkManager.Singleton;
        if (networkManager == null)
        {
            Debug.LogError("NetworkManager.Singleton 为 null，无法设置连接数据！");
            return;
        }
        
        if (!networkManager.TryGetComponent<UnityTransport>(out var transport))
        {
            Debug.LogWarning("未找到UnityTransport组件！请确保NetworkManager上挂载了UnityTransport。");
            return;
        }
        
        if (listenOnAllInterfaces)
        {
            // Host/Server：监听 0.0.0.0，这样其他电脑才能连进来
            transport.SetConnectionData(ip, port, "0.0.0.0");
            Debug.Log($"设置连接数据（服务器监听所有网卡）: {ip}:{port}，监听 0.0.0.0");
        }
        else
        {
            // Client：连接到指定 IP:端口
            transport.SetConnectionData(ip, port);
            Debug.Log($"设置连接数据（客户端）: {ip}:{port}");
        }
    }
}

