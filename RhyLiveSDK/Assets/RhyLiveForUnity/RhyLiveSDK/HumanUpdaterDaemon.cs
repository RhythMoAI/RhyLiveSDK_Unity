using System;
using System.Threading.Tasks;
using System.ServiceProcess;
using System.Runtime.InteropServices;
using UnityEngine;

using extOSC;
using iMobileDevice;

namespace RhythMo.RhyLiveSDK {
//This Script Receives MotionData and awakes Updaters
//此脚本负责接收动作数据并唤醒Updater脚本
public class HumanUpdaterDaemon : MonoBehaviour
{
    [SerializeField]
    public FaceUpdater faceUpdater;

    [SerializeField]
    public PoseUpdater poseUpdater;
    
    // Udp_Osc
    [SerializeField]
    bool autoStartOsc;
    
    [SerializeField]
    bool autoStartUsb;
    
    [SerializeField]
    ushort autoStartOscPort;

    [SerializeField]
    ushort autoStartUsbPort;

    private OSCReceiver udpOscReceiver;
    private OscHumanUpdater oscUpdater;

    // Usb_iOS
    private GCHandle iOSReceiverHandle;
    private RawHumanUpdater rawUpdater;

    private void Awake() {
        StopHumanUpdater();

        // init iDeviceMobile
        //初始化苹果usb接收服务
        oscUpdater = gameObject.AddComponent<OscHumanUpdater>();
        oscUpdater.faceUpdater = this.faceUpdater;
        oscUpdater.poseUpdater = this.poseUpdater;

        rawUpdater = gameObject.AddComponent<RawHumanUpdater>();
        rawUpdater.poseUpdater = this.poseUpdater;
        rawUpdater.faceUpdater = this.faceUpdater;
    }
    //Start data receiving
    //开始动作数据接收
    private void Start() {
        //Using osc receive
        //无线采用osc接收
        if (autoStartOsc) {
            StartUdp_Osc(autoStartOscPort);
        }
        //Using usb receive (ios only)
        //有线采用usb接收（目前只支持ios）
        if (autoStartUsb) {
            StartUsb_iOS(autoStartUsbPort);
        }
    }

    private void Update() {
        if(iOSReceiverHandle.IsAllocated) {
            (iOSReceiverHandle.Target as UsbiOSReceiver).PollUsbConnection();
        }
    }

    private void OnDestroy() {
        StopUsb_iOS();
        StopUdp_Osc();
    }

    public void StartUdp_Osc(ushort port) {
        StartHumanUpdater();

        udpOscReceiver = gameObject.GetComponent<OSCReceiver>(); 
        if (udpOscReceiver == null) {
            udpOscReceiver = gameObject.AddComponent<OSCReceiver>();
        }
        udpOscReceiver.LocalPort = (int)port;
        udpOscReceiver.Bind("/Face", oscUpdater.UpdateFace);
        udpOscReceiver.Bind("/Body", oscUpdater.UpdatePose);
        udpOscReceiver.Connect();
    }

    public bool StopUdp_Osc() {
        bool shouldStop = (udpOscReceiver != null);
        if (shouldStop) {
            udpOscReceiver.ClearBinds();
            udpOscReceiver.Close();
            udpOscReceiver = null;
        }

        StopHumanUpdater();
        return shouldStop;
    }

    public void StartUsb_iOS(ushort port) {
        StartHumanUpdater();

        
        var usbmuxd = LibiMobileDevice.Instance.Usbmuxd;
        var idevice = LibiMobileDevice.Instance.iDevice;
        var appleDeviceService = UsbiOSReceiver.DetectAndStartAppleDeviceService();
        // require update
        var iOSReceiver = new UsbiOSReceiver(); 
        iOSReceiver.RawUpdater = rawUpdater.UpdateData;
        iOSReceiver.RemotePort = port;

        iOSReceiverHandle = GCHandle.Alloc(iOSReceiver); // iOSReceiver is null now
        IntPtr receiverPtr = GCHandle.ToIntPtr(iOSReceiverHandle);

        if (appleDeviceService != null) { // wait until the service is started
            Task.Run(async() => {
                await Task.Delay(3000);
                Debug.Log("[RhyLive SDK] 正在等待苹果服务启动, 请稍后");
                appleDeviceService.WaitForStatus(ServiceControllerStatus.Running, new TimeSpan(0, 0, 15));

                if (appleDeviceService.Status != ServiceControllerStatus.Running) {
                    Debug.LogError("[RhyLive SDK] 苹果服务启动超时, 请尝试手动启动 Apple Mobile Device Service 服务并重启有线连接开关");
                } else {
                    int errorRet = usbmuxd.usbmuxd_subscribe(UsbiOSReceiver.UsbiOS_OnDeviceChange, receiverPtr);
                    // var errorRet = idevice.idevice_event_subscribe(UsbiOSReceiver.UsbiOS_OnDeviceChange, receiverPtr);
                    if (errorRet != 0) {
                        iOSReceiverHandle.Free();
                        Debug.LogError("[RhyLive SDK] iOS设备监听失败, 未知错误: " + errorRet.ToString());
                    } else {
                        Debug.Log("[RhyLive SDK] 正在等待iOS设备连接, 如已经连接, 请尝试先断开再重新连接");
                    }
                }
            });
        } else { // start immediately
            int errorRet = usbmuxd.usbmuxd_subscribe(UsbiOSReceiver.UsbiOS_OnDeviceChange, receiverPtr);
            // var errorRet = idevice.idevice_event_subscribe(UsbiOSReceiver.UsbiOS_OnDeviceChange, receiverPtr);
            if (errorRet != 0) {
                iOSReceiverHandle.Free();
                Debug.LogError("[RhyLive SDK] iOS设备监听失败, 未知错误: " + errorRet.ToString());
            } else {
                Debug.Log("[RhyLive SDK] 正在等待iOS设备连接");
            }
        }
    }

    public bool StopUsb_iOS() {
        bool shouldStop = iOSReceiverHandle.IsAllocated;
        if (shouldStop) {
            var iOSReceiver = iOSReceiverHandle.Target as UsbiOSReceiver;
            iOSReceiver.DisconnectUsb_iOS(true);
            LibiMobileDevice.Instance.Usbmuxd.usbmuxd_unsubscribe();
            iOSReceiverHandle.Free();
            // LibiMobileDevice.Instance.iDevice.idevice_event_unsubscribe();
        }

        StopHumanUpdater();
        return shouldStop;
    }

    private void StartHumanUpdater() {
        if (this.faceUpdater) this.faceUpdater.enabled = true;
        if (this.poseUpdater) this.poseUpdater.enabled = true;
    }

    private void StopHumanUpdater() {
        if (this.faceUpdater) this.faceUpdater.enabled = false;
        if (this.poseUpdater) this.poseUpdater.enabled = false;
    }
}

}