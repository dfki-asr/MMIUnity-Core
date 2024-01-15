using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using MMICSharp.Adapter;
using CSharpAdapter;
using MMICSharp.Common;
using MMIStandard;


namespace MMIUnity
{
    class MMUWrapper
    {
        public GameObject go;
        public string mmuID;
        public IMotionModelUnitDev mmu;
        // initialization info
        public MAvatarDescription avatarDesc;
        public Dictionary<string, string> props;
    }

    class SessionWrapper
    {
        public Dictionary<string, MMUWrapper> baseMMUs = new Dictionary<string, MMUWrapper>();
        public Dictionary<string, MMUWrapper> instancesMMUs = new Dictionary<string, MMUWrapper>();
        public Transform parent;

        public SessionWrapper(string sessionID)
        {
            parent = new GameObject(sessionID).transform;
        }
    }

    public class UnityAdapterImplementation : ThriftAdapterImplementation
    {
        Dictionary<string, SessionWrapper> sessions = new Dictionary<string, SessionWrapper>();

        public UnityAdapterImplementation(SessionData sessionData, IMMUInstantiation mmuInstantiator) : base(sessionData, mmuInstantiator)
        {

        }
        public override MBoolResponse Initialize(MAvatarDescription avatarDescription, Dictionary<string, string> properties, string mmuID, string sessionID)
        {
            MAvatarDescription avDesc = avatarDescription.Clone();

            MBoolResponse initialization = base.Initialize(avatarDescription, properties, mmuID, sessionID);

            MainThreadDispatcher.Instance.ExecuteBlocking(delegate
            {
                if (!sessions.ContainsKey(sessionID))
                {
                    sessions.Add(sessionID, new SessionWrapper(sessionID));
                }
            });

            // add base MMU
            SessionContent sessionContent = null;
            AvatarContent avatarContent = null;

            MBoolResponse sessionResult = this.SessionData.GetContents(sessionID, out sessionContent, out avatarContent);
            if (!sessionResult.Successful)
                return sessionResult;

            var mmu = avatarContent.MMUs[mmuID];
            GameObject mmuGO = null;
            MainThreadDispatcher.Instance.ExecuteBlocking(delegate
            {
                UnityMMUBase[] mmus = GameObject.FindObjectsOfType<UnityMMUBase>();
                //if (mmus.Length == 0) Debug.Log("No MMUs");
                foreach(var m in mmus)
                {
                    //Debug.Log($"checking {m.ID} against {mmuID}");
                    if(m.Name == mmu.Name && !sessions[sessionID].instancesMMUs.ContainsKey(m.gameObject.name))
                    {
                        mmuGO = m.gameObject;
                        mmuGO.transform.parent = sessions[sessionID].parent;
                        break;
                    }
                }
            });
            if (mmuGO == null)
            {
                MMICSharp.Logger.LogError($"Could not find MMU Game Object {mmu.Name}(Clone)");
            }

            sessions[sessionID].baseMMUs.Add(mmuID, new MMUWrapper() { go = mmuGO, mmuID = mmuID, mmu = mmu, avatarDesc = avDesc, props = properties });

            return initialization;
        }


        public override MBoolResponse AssignInstruction(MInstruction instruction, MSimulationState simulationState, string mmuID, string sessionID)
        {
            // get session data
            SessionContent sessionContent = null;
            AvatarContent avatarContent = null;

            MBoolResponse sessionResult = this.SessionData.GetContents(sessionID, out sessionContent, out avatarContent);
            if (!sessionResult.Successful)
                return sessionResult;

            string newID = "";
            var mmuw = sessions[sessionID].baseMMUs[mmuID];
            newID = $"{mmuw.mmuID}:{instruction.ID}";
            IMotionModelUnitDev newMMU = null;
            if (!sessions[sessionID].instancesMMUs.ContainsKey(newID))
            {
                // create new instance
                MainThreadDispatcher.Instance.ExecuteBlocking(delegate
                {
                    var newGO = GameObject.Instantiate(mmuw.go);
                    newGO.name = newID;
                    newGO.transform.parent = sessions[sessionID].parent;
                    newMMU = newGO.GetComponent<UnityMMUBase>();
                    // add new Instance
                    sessions[sessionID].instancesMMUs.Add(newID, new MMUWrapper() { go = newGO, mmu = newMMU, mmuID = newID });
                    avatarContent.MMUs.Add(newID, newMMU);
                });
                //newMMU.ServiceAccess = new ServiceAccess(SessionData.MMIRegisterAddress, sessionID);
                //((ServiceAccess)newMMU.ServiceAccess).Initialize();
                newMMU.ServiceAccess = mmuw.mmu.ServiceAccess;
                newMMU.SceneAccess = mmuw.mmu.SceneAccess;
                newMMU.AdapterEndpoint = mmuw.mmu.AdapterEndpoint;

                base.Initialize(sessions[sessionID].baseMMUs[mmuID].avatarDesc, sessions[sessionID].baseMMUs[mmuID].props, newID, sessionID);
            }
            else
            {
                MMICSharp.Logger.LogInfo($"(UnityAdapter) Instance {newID} already existing. ");
            }

            return base.AssignInstruction(instruction, simulationState, newID, sessionID);
        }

        public override MBoolResponse CheckPrerequisites(MInstruction instruction, string mmuID, string sessionID)
        {
            // $"{mmuID}:{instruction.ID}"
            return base.CheckPrerequisites(instruction, mmuID, sessionID);
        }

        public override List<MConstraint> GetBoundaryConstraints(MInstruction instruction, string mmuID, string sessionID)
        {
            var boundaries = base.GetBoundaryConstraints(instruction, mmuID, sessionID);
            return boundaries;
        }

        public override MSimulationResult DoStep(double time, MSimulationState simulationState, string mmuID, string sessionID)
        {
            // get all instanceMMUs. 
            List<MMUWrapper> inst = new List<MMUWrapper>();
            foreach(var key in sessions[sessionID].instancesMMUs.Keys)
            {
                if(key.Split(":")[0] == mmuID)
                {
                    inst.Add(sessions[sessionID].instancesMMUs[key]);
                }
            }

            // run all MMUs after oneanother. 
            MSimulationResult result = new MSimulationResult()
            {
                Posture = simulationState.Current,
                Constraints = new List<MConstraint>(),
                Events = new List<MSimulationEvent>(),
                //Constraints = simulationState.Constraints,
                //Events = simulationState.Events,
                DrawingCalls = new List<MDrawingCall>(),
                LogData = new List<string>(),
                SceneManipulations = simulationState.SceneManipulations
            };
            foreach (MMUWrapper muw in inst)
            {
                MSimulationResult r = base.DoStep(time, simulationState, muw.mmuID, sessionID);
                result.Posture = r.Posture;
                if(r.LogData != null)
                    result.LogData.AddRange(r.LogData);
                if(r.Events!= null)
                    result.Events.AddRange(r.Events);
                if(r.SceneManipulations != null)
                    result.SceneManipulations.AddRange(r.SceneManipulations);
                if(r.Constraints != null)
                    result.Constraints.AddRange(r.Constraints);
                if(r.DrawingCalls != null)
                    result.DrawingCalls.AddRange(r.DrawingCalls);

                // TODO: avoid accumulation of constraints and events. 
                //simulationState.Constraints.AddRange(r.Constraints);
                simulationState.Current = r.Posture;
                //simulationState.Events.AddRange(r.Events);

            }
            // Check for end event and remove instance
            if (result.Events != null)
            {
                foreach (MSimulationEvent e in result.Events)
                {
                    if (e.Type == mmiConstants.MSimulationEvent_End)
                    {
                        foreach(MMUWrapper muw in inst)
                        {
                            if(muw.mmuID.Split(":")[1] == e.Reference)
                            {
                                RemoveInstMMU(muw, sessionID);
                                break;
                            }
                                
                        }
                        // TODO: should I call dispose here? 
                        
                    }
                }
            }

            return result;
        }

        private void RemoveInstMMU(MMUWrapper instMMU, string sessionID)
        {
            // get session data
            SessionContent sessionContent = null;
            AvatarContent avatarContent = null;

            MBoolResponse sessionResult = this.SessionData.GetContents(sessionID, out sessionContent, out avatarContent);
            if (sessionResult.Successful)
            {
                avatarContent.MMUs.Remove(instMMU.mmuID);
            }
            MainThreadDispatcher.Instance.ExecuteBlocking(delegate
            {
                GameObject.Destroy(instMMU.go);
            });
            sessions[sessionID].instancesMMUs.Remove(instMMU.mmuID);
        }

        public override MBoolResponse Abort(string instructionId, string mmuID, string sessionID)
        {
            var instMMU = sessions[sessionID].instancesMMUs[$"{mmuID}:{instructionId}"];

            MBoolResponse resp = base.Abort(instructionId, instMMU.mmuID, sessionID);
            RemoveInstMMU(instMMU, sessionID);
            return resp;
        }

        public override MBoolResponse CloseSession(string sessionID)
        {
            MBoolResponse r = base.CloseSession(sessionID);

            MainThreadDispatcher.Instance.ExecuteBlocking(delegate
            {
                // cleanup unity objects
                var s = this.sessions[sessionID];
                foreach (var inst in s.instancesMMUs.Keys)
                {
                    GameObject.Destroy(s.instancesMMUs[inst].go);
                }
                s.instancesMMUs.Clear();
                foreach (var inst in s.baseMMUs.Keys)
                {
                    GameObject.Destroy(s.baseMMUs[inst].go);
                }
                GameObject.Destroy(s.parent.gameObject);
                s.baseMMUs.Clear();
                this.sessions.Remove(sessionID);
            });
            return r;
        }

    }
}