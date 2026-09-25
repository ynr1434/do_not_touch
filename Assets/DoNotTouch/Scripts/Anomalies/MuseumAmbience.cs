using UnityEngine;
namespace DoNotTouch {
 // Small, generated PCM clips: no downloads, plugins or external audio dependencies.
 public class MuseumAmbience : MonoBehaviour {
  public Transform[] roomSources;
  AudioSource tone,vent,buzz,events,threat,alarm;
  AudioClip humClip,ventClip,buzzClip,creakClip,impactClip,relayClip,scrapeClip,alarmClip;
  int heardSerial,heardMove;
  public bool Ready=>humClip && ventClip && buzzClip && creakClip && impactClip && relayClip && scrapeClip && alarmClip;
  public void Begin(HorrorDirector director) {
   if(!Ready) {
    humClip=Make("Room tone",8,0);ventClip=Make("Ventilation",8,1);buzzClip=Make("Electrical buzz",8,2);
    creakClip=Make("Distant creak",2.5f,3);impactClip=Make("Distant knock",1.8f,4);relayClip=Make("Lamp relay",.8f,5);scrapeClip=Make("Stone scraping",3.5f,6);alarmClip=Make("Distant alarm",4,7);
    tone=Source("Room tone",Vector3.zero,false);tone.clip=humClip;tone.loop=true;tone.volume=.065f;
    vent=Source("Air duct",roomSources[3].position,true);vent.clip=ventClip;vent.loop=true;vent.volume=.2f;
    buzz=Source("Corridor ballast",roomSources[1].position,true);buzz.clip=buzzClip;buzz.loop=true;buzz.volume=.10f;
    events=Source("Distant museum event",roomSources[4].position,true);events.volume=.30f;events.maxDistance=28;
    threat=Source("Watcher stone movement",Vector3.zero,true);threat.clip=scrapeClip;threat.loop=false;threat.volume=.22f;threat.maxDistance=18;
    alarm=Source("Distant closing alarm",roomSources[0].position,true);alarm.clip=alarmClip;alarm.loop=true;alarm.volume=0;alarm.maxDistance=35;alarm.Play();
    var reverb=events.gameObject.AddComponent<AudioReverbFilter>();reverb.reverbPreset=AudioReverbPreset.StoneCorridor;
   }
   heardSerial=director.EventSerial.Value;heardMove=director.watcher?director.watcher.MoveCount.Value:0;tone.Play();vent.Play();buzz.Play();
  }
  AudioSource Source(string name,Vector3 position,bool spatial) {
   var go=new GameObject(name);go.transform.SetParent(transform);go.transform.position=position;
   var source=go.AddComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=spatial?1:0;source.rolloffMode=AudioRolloffMode.Logarithmic;source.minDistance=2;source.maxDistance=22;source.dopplerLevel=0;return source;
  }
  public void Tick(HorrorDirector director) {
   if(!Ready)return;
   bool safe=PlayerController.Local && ShiftManager.Instance.exitZone.Contains(PlayerController.Local.transform.position);
   tone.volume=Mathf.Lerp(tone.volume,safe?.045f:.065f,Time.deltaTime);
   buzz.volume=Mathf.Lerp(buzz.volume,safe?.025f:.10f,Time.deltaTime);
   var watcher=director.watcher;float proximity=0;if(PlayerController.Local&&watcher&&watcher.Activated.Value){float distance=Vector3.Distance(PlayerController.Local.transform.position,watcher.transform.position);proximity=1-Mathf.InverseLerp(3,18,distance);threat.transform.position=watcher.transform.position;}
   if(watcher&&watcher.MoveCount.Value!=heardMove){heardMove=watcher.MoveCount.Value;threat.volume=safe?.08f:.22f;threat.PlayOneShot(scrapeClip);events.PlayOneShot(relayClip,.2f);}
   bool final=NightShiftDirector.Instance&&NightShiftDirector.Instance.Phase.Value>=(int)NightPhase.FinalTask;alarm.volume=Mathf.Lerp(alarm.volume,final?.07f:0,Time.deltaTime*.5f);
   if(heardSerial==director.EventSerial.Value)return;
   heardSerial=director.EventSerial.Value;
   double age=director.Now-director.EventStart.Value;
   if(age>1 || heardSerial==0)return; // Don't replay old sounds on late join.
   events.transform.position=roomSources[director.EventRoom.Value].position;
   var kind=(HorrorEventKind)director.EventKind.Value;
   events.clip=kind==HorrorEventKind.Creak?creakClip:kind==HorrorEventKind.DistantImpact?impactClip:relayClip;
   events.volume=safe?.075f:.30f;events.pitch=.92f+(heardSerial%5)*.035f;
   events.PlayScheduled(AudioSettings.dspTime+System.Math.Max(0,-age));
  }
  public void Stop(){if(tone)tone.Stop();if(vent)vent.Stop();if(buzz)buzz.Stop();if(events)events.Stop();if(threat)threat.Stop();if(alarm)alarm.Stop();}
  void OnDestroy(){foreach(var clip in new[]{humClip,ventClip,buzzClip,creakClip,impactClip,relayClip,scrapeClip,alarmClip})if(clip)Destroy(clip);}
  static AudioClip Make(string name,float seconds,int kind) {
   const int rate=22050;int count=(int)(rate*seconds);var samples=new float[count];var random=new System.Random(2417+kind);float filtered=0;
   for(int i=0;i<count;i++) {
    float t=(float)i/rate;float noise=(float)random.NextDouble()*2-1;filtered=Mathf.Lerp(filtered,noise,.018f);
    float value=0;
    switch(kind) {
     case 0:value=Mathf.Sin(2*Mathf.PI*48*t)*.18f+Mathf.Sin(2*Mathf.PI*61*t)*.08f+filtered*.25f;break;
     case 1:value=filtered*.7f+Mathf.Sin(2*Mathf.PI*83*t)*.05f;break;
     case 2:value=Mathf.Sin(2*Mathf.PI*50*t)*.13f+Mathf.Sin(2*Mathf.PI*150*t)*.035f+noise*.01f;break;
     case 3:value=(Mathf.Sin(2*Mathf.PI*(110*t+28*t*t))*.3f+filtered*.4f)*Mathf.Sin(Mathf.PI*t/seconds)*(.65f+.35f*Mathf.Sin(t*17));break;
     case 4:value=(Mathf.Sin(2*Mathf.PI*72*t)*.6f+noise*.3f)*Mathf.Exp(-t*7);break;
     case 5:value=noise*.25f*Mathf.Exp(-t*32)+Mathf.Sin(2*Mathf.PI*100*t)*.12f*Mathf.Exp(-t*8);break;
     case 6:value=(filtered*.55f+Mathf.Sin(2*Mathf.PI*(42+t*4)*t)*.18f)*(.65f+.35f*Mathf.Sin(t*4.1f));break;
     case 7:value=(Mathf.Sin(2*Mathf.PI*330*t)+Mathf.Sin(2*Mathf.PI*440*t))*.12f*(.55f+.45f*Mathf.Sin(t*2.2f));break;
    }
    float edge=Mathf.Min(1,Mathf.Min(t,seconds-t)*30);samples[i]=Mathf.Clamp(value*edge,-.8f,.8f);
   }
   var clip=AudioClip.Create(name,count,1,rate,false);clip.SetData(samples,0);return clip;
  }
 }
}
