using System;
using System.Reflection;
using GameFoundation.Saves;
using UnityEditor;
using UnityEngine;

public static class UnfinishedRunValidation
{
    const int Slot=996571;
    static readonly Type Service=typeof(SaveSlotPrefs).Assembly.GetType("GameFoundation.Saves.GameSaveService");
    static object Call(string method,params object[] args)=>Service.GetMethod(method,BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,args);
    static void Put(string key,string value)=>Call("Set",Slot,key,"int",value);
    static int Read(string key)=> (int)Call("GetInt",Slot,key,-1);
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static void Run()
    {
        string envelope="foundation.slot."+Slot+".saveData",legacy="foundation.slot."+Slot+".legacy";
        if(PlayerPrefs.HasKey(envelope)||PlayerPrefs.HasKey(legacy))throw new Exception("QA save namespace already exists; left untouched");
        var suspended=Service.GetField("WritesSuspended",BindingFlags.Static|BindingFlags.NonPublic);
        bool previous=(bool)suspended.GetValue(null);suspended.SetValue(null,false);
        try
        {
            Put("Human","7");Put("Cart","2");Put("Wood","5");Put("Ore","30");Put("entered","0");Put("quest","0");Call("Save",Slot);
            PlayerPrefs.SetInt(legacy,19);PlayerPrefs.Save();
            Check((bool)Call("BeginRunCheckpoint",Slot),"Checkpoint must start before departure");
            Check(!(bool)Call("BeginRunCheckpoint",Slot),"A second Begin must not overwrite the shelter snapshot");
            Put("Human","0");Put("Cart","0");Put("Wood","100");Put("Ore","90");Put("entered","1");Put("quest","1");Put("cartDebt","4");
            Call("Delete",Slot,"legacy");Call("Save",Slot);
            Check(Read("Human")==0&&Read("Wood")==100,"Battle must see live gains and losses in memory");
            Check(!(bool)Call("HasKey",Slot,"legacy"),"Battle deletions must be visible in memory");
            Call("ResetRunSession"); // Simulate process loss: no completion callback executes.
            Check(Read("Human")==7&&Read("Cart")==2&&Read("Wood")==5&&Read("Ore")==30&&Read("entered")==0&&Read("quest")==0&&Read("cartDebt")==-1,
                "Unfinished battle must restore all shelter state without gains or losses");
            Check((int)Call("GetInt",Slot,"legacy",0)==19,"Interrupted deletion must preserve legacy baseline");
            string recovered=PlayerPrefs.GetString(envelope);Call("ResetRunSession");Check(Read("Human")==7&&PlayerPrefs.GetString(envelope)==recovered,"Recovery must be idempotent across repeated launches");
            Call("BeginRunCheckpoint",Slot);Put("Human","6");Put("Cart","1");Put("Wood","20");Put("entered","1");Call("Delete",Slot,"legacy");Call("CompleteRunCheckpoint",Slot);
            Call("ResetRunSession");
            Check(Read("Human")==6&&Read("Cart")==1&&Read("Wood")==20&&Read("entered")==1,"Completed battle must preserve actual gains and losses");
            Check(!(bool)Call("HasKey",Slot,"legacy")&&(int)Call("GetInt",Slot,"legacy",77)==77,"Committed tombstone must not resurrect legacy data");
            Call("BeginRunCheckpoint",Slot);Put("Wood","99");Call("Save",Slot);Call("ResetRunSession");Check(Read("Wood")==20,"Next interrupted run must restore the latest completed base state");
            SessionState.SetString("UnfinishedRunQA","PASS: isolated slot, duplicate Begin, live run state, abrupt restart rollback, full flags/resources, legacy deletions, repeated recovery, completed run commit and next run");
        }
        finally
        {
            Call("ResetRunSession");PlayerPrefs.DeleteKey(envelope);PlayerPrefs.DeleteKey(legacy);PlayerPrefs.Save();suspended.SetValue(null,previous);
        }
    }
}
