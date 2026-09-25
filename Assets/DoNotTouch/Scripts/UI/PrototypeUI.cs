using UnityEngine;
using UnityEngine.UI;
using System;
namespace DoNotTouch {
 public class PrototypeUI : MonoBehaviour {
  public NetworkSession session;
  public Camera lobbyCamera;
  Text clock,objectives,prompt,status,summary,history;
  GameObject menu,hud,pause,result,blackout,historyPanel;
  InputField address;
  Font font;
  float nextRefresh;
  string previousStep;float taskFeedbackUntil;
  void Awake(){Build();}
  Text Label(Transform parent,string value,Vector2 pos,Vector2 size,int fontSize=22) {
   var go=new GameObject("Text",typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);
   var r=go.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=pos;r.sizeDelta=size;
   var t=go.GetComponent<Text>();t.font=font;t.fontSize=fontSize;t.text=value;t.color=new Color(.88f,.91f,.87f);t.raycastTarget=false;return t;
  }
  GameObject Panel(string name,Vector2 pos,Vector2 size) {
   var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(transform,false);
   var r=go.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=pos;r.sizeDelta=size;
   go.GetComponent<Image>().color=new Color(.035f,.055f,.065f,.95f);return go;
  }
  void Button(Transform parent,string text,Vector2 pos,UnityEngine.Events.UnityAction action) {
   var go=new GameObject(text,typeof(RectTransform),typeof(Image),typeof(Button));go.transform.SetParent(parent,false);
   var r=go.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=pos;r.sizeDelta=new Vector2(230,48);
   go.GetComponent<Image>().color=new Color(.16f,.28f,.27f);var b=go.GetComponent<Button>();b.onClick.AddListener(action);
   Label(go.transform,text,new Vector2(12,-10),new Vector2(210,34),20);
  }
  void Build() {
   font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
   menu=Panel("Connection menu",Vector2.zero,new Vector2(650,530));
   Label(menu.transform,"DO NOT TOUCH",new Vector2(35,-30),new Vector2(590,65),42);
   Label(menu.transform,"Музей закрыт. Экспонаты — нет.",new Vector2(35,-100),new Vector2(590,40),21);
   Label(menu.transform,"НОЧНАЯ СМЕНА / 1–4 СОТРУДНИКА",new Vector2(35,-157),new Vector2(590,40),23);
   Button(menu.transform,"Начать смену",new Vector2(35,-225),()=>session.Host());
   Button(menu.transform,"Подключиться",new Vector2(350,-225),()=>session.Join(address.text));
   var entry=new GameObject("Host IP",typeof(RectTransform),typeof(Image),typeof(InputField));entry.transform.SetParent(menu.transform,false);
   var er=entry.GetComponent<RectTransform>();er.anchorMin=er.anchorMax=new Vector2(0,1);er.pivot=new Vector2(0,1);er.anchoredPosition=new Vector2(350,-290);er.sizeDelta=new Vector2(230,44);
   entry.GetComponent<Image>().color=new Color(.12f,.15f,.16f);
   address=entry.GetComponent<InputField>();address.textComponent=Label(entry.transform,"",new Vector2(10,-8),new Vector2(210,32),20);address.text="127.0.0.1";address.characterLimit=45;
   status=Label(menu.transform,"",new Vector2(35,-360),new Vector2(590,70),19);
   Label(menu.transform,"Выполните задания и соберитесь в комнате персонала.\nWASD — ходьба · E — действие · ЛКМ — взять · F — фонарик",new Vector2(35,-443),new Vector2(590,65),17);
   hud=new GameObject("HUD",typeof(RectTransform));hud.transform.SetParent(transform,false);
   var hr=hud.GetComponent<RectTransform>();hr.anchorMin=Vector2.zero;hr.anchorMax=Vector2.one;hr.offsetMin=hr.offsetMax=Vector2.zero;
   clock=Label(hud.transform,"00:00",new Vector2(30,-25),new Vector2(350,50),30);
   objectives=Label(hud.transform,"",new Vector2(30,-85),new Vector2(520,230),21);
   historyPanel=Panel("Objective history",new Vector2(350,40),new Vector2(500,430));
   history=Label(historyPanel.transform,"",new Vector2(30,-28),new Vector2(440,370),21);
   historyPanel.SetActive(false);
   prompt=Label(hud.transform,"",new Vector2(260,-600),new Vector2(760,100),20);prompt.alignment=TextAnchor.MiddleCenter;
   var cross=Label(hud.transform,"+",new Vector2(630,-340),new Vector2(30,40),25);cross.alignment=TextAnchor.MiddleCenter;
   Label(hud.transform,"TAB — задачи  ESC — меню",new Vector2(1010,-25),new Vector2(260,35),17);
   pause=Panel("Pause",Vector2.zero,new Vector2(450,280));
   Label(pause.transform,"СМЕНА ПРОДОЛЖАЕТСЯ",new Vector2(25,-25),new Vector2(400,45),23);
   Button(pause.transform,"Продолжить",new Vector2(100,-100),()=>PlayerController.Local.SetPause(false));
   Button(pause.transform,"Покинуть смену",new Vector2(100,-170),()=>session.Restart());
   result=Panel("Shift result",Vector2.zero,new Vector2(600,430));
   summary=Label(result.transform,"",new Vector2(35,-30),new Vector2(530,290),25);
   Button(result.transform,"Новая смена",new Vector2(180,-345),()=>session.Restart());
#if UNITY_EDITOR || DEVELOPMENT_BUILD
   if(Array.IndexOf(Environment.GetCommandLineArgs(),"-dnt-qa-menu")>=0)Button(menu.transform,"Сбросить onboarding",new Vector2(35,-505),()=>NightShiftDirector.Instance?.ResetOnboarding());
#endif
   blackout=new GameObject("Watcher blackout",typeof(RectTransform),typeof(Image));blackout.transform.SetParent(transform,false);var br=blackout.GetComponent<RectTransform>();br.anchorMin=Vector2.zero;br.anchorMax=Vector2.one;br.offsetMin=br.offsetMax=Vector2.zero;blackout.GetComponent<Image>().color=Color.black;
  }
  void Update() {
   if(Time.unscaledTime<nextRefresh)return;nextRefresh=Time.unscaledTime+.08f;
   var player=PlayerController.Local;bool playing=player && player.IsSpawned;
   menu.SetActive(!playing);hud.SetActive(playing);lobbyCamera.gameObject.SetActive(!playing);blackout.SetActive(playing&&player.IsSpawned&&player.NetworkManager.ServerTime.Time<player.IncapacitatedUntil.Value);historyPanel.SetActive(playing&&player.ShowObjectives);
   pause.SetActive(playing&&player.Paused);result.SetActive(false);status.text=session.Status;
   if(!playing)return;
   var shift=ShiftManager.Instance;
   clock.text="НОЧНАЯ СМЕНА   "+ShiftRules.Clock(shift.Elapsed.Value,shift.definition.durationSeconds);
   var night=NightShiftDirector.Instance;
   objectives.text=night?night.ObjectiveText(shift):$"[{shift.Crates.Value}/{shift.definition.crateTarget}] {shift.definition.crateTask}\n\n[{shift.Inspections.Value}/{shift.definition.inspectionTarget}] {shift.definition.inspectTask}\n\n[{shift.Statues.Value}/{shift.definition.statueTarget}] {shift.definition.statueTask}";
   history.text=night?night.HistoryText(shift):objectives.text;
   if(!night&&shift.AllTasks)objectives.text+="\n\nВсе сотрудники: вернитесь в комнату персонала.";
   bool holding=false;foreach(var p in PickupObject.Active)if(p.IsHeld && p.Holder.Value==player.OwnerClientId){holding=true;break;}
   if(night){string step=night.CurrentStep(shift);if(previousStep!=null&&previousStep!=step)taskFeedbackUntil=Time.unscaledTime+3;previousStep=step;}
   prompt.text=Time.unscaledTime<player.ScanMessageUntil?player.ScanMessage:Time.unscaledTime<taskFeedbackUntil?"Задание выполнено.":holding?"[ЛКМ / E] Положить  [ПКМ] Бросить":player.Prompt;
   if(shift.Result.Value!=0){
    result.SetActive(true);pause.SetActive(false);player.SetPause(true);
    summary.text=(shift.Result.Value==1?"СМЕНА ЗАВЕРШЕНА":"СМЕНА НЕ ЗАВЕРШЕНА")+$"\n\nЭтап: {(night ? night.Phase.Value + 1 : shift.TasksCompleted)} / {(night ? 7 : 3)}\nВремя: {ShiftRules.Clock(shift.Elapsed.Value,shift.definition.durationSeconds)}\nПовреждено предметов: {shift.Damaged.Value}\nЗамечено аномалий: {shift.Witnessed.Value}\n\nАдминистрация благодарит за работу.";
   }
  }
 }
}
