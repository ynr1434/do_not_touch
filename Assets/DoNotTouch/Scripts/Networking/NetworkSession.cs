using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace DoNotTouch {
 public class NetworkSession : MonoBehaviour {
  public NetworkManager manager;
  public UnityTransport transport;
  public string Status {get;private set;}="Начните смену или подключитесь к ведущему.";
  public bool Connecting {get;private set;}
  public ushort port=7777;
  void Awake() {
   manager.ConnectionApprovalCallback=Approve;
   manager.OnClientConnectedCallback+=Connected;
   manager.OnClientDisconnectCallback+=Disconnected;
   manager.OnTransportFailure+=TransportFailure;
  }
  void OnDestroy(){if(manager){manager.OnClientConnectedCallback-=Connected;manager.OnClientDisconnectCallback-=Disconnected;manager.OnTransportFailure-=TransportFailure;}}
  void Approve(NetworkManager.ConnectionApprovalRequest req,NetworkManager.ConnectionApprovalResponse res) {
   res.Approved=manager.ConnectedClients.Count<4 && (!ShiftManager.Instance || ShiftManager.Instance.Result.Value==0);
   if(res.Approved && ShiftManager.Instance) {
    var spawns=ShiftManager.Instance.spawnPoints;
    res.Position=spawns[(int)(req.ClientNetworkId%(ulong)spawns.Length)].position;
    res.Rotation=Quaternion.identity;
   }
   res.CreatePlayerObject=true;res.Pending=false;res.Reason=res.Approved?"":"Смена заполнена или уже завершена.";
  }
  public void Host() {if(manager.IsListening||Connecting)return;transport.SetConnectionData("127.0.0.1",port,"0.0.0.0");Status=manager.StartHost()?"Сервер: порт "+port:"Не удалось запустить сервер.";}
  public void Join(string address) {
   if(manager.IsListening||Connecting)return;
   if(!System.Net.IPAddress.TryParse(address.Trim(),out _)){Status="Введите IP-адрес ведущего.";return;}
   transport.SetConnectionData(address.Trim(),port);Connecting=manager.StartClient();Status=Connecting?"Подключение…":"Не удалось подключиться.";
  }
  void Connected(ulong id){if(id==manager.LocalClientId){Connecting=false;Status="Подключено";}}
  void Disconnected(ulong id){if(id==manager.LocalClientId){Connecting=false;Status="Соединение прервано. "+manager.DisconnectReason;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}}
  void TransportFailure(){Connecting=false;Status="Ошибка сети. Проверьте порт 7777 и перезапустите смену.";}
  public async void Restart(){
   int scene=SceneManager.GetActiveScene().buildIndex;
   manager.Shutdown();await Awaitable.NextFrameAsync();
   Destroy(manager.gameObject);await Awaitable.NextFrameAsync();
   SceneManager.LoadScene(scene);
  }
 }
}
