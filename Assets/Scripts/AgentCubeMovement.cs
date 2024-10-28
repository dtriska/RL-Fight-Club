//Standardized movement controller for the Agent Cube
using Unity.MLAgents;
using UnityEngine;

namespace MLAgents
{
    public class AgentCubeMovement : MonoBehaviour
    {

        //ONLY ALLOW SCRIPTED MOVEMENT VIA ML-AGENTS OR OTHER HEURISTIC SCRIPTS
        [Header("INPUT")]
        public bool allowHumanInputAndDisableAgentHeuristicInput = true;

        [Header("RIGIDBODY")] public float maxAngularVel = 50;
        [Header("RUNNING")] public ForceMode runningForceMode = ForceMode.Impulse;
        public float agentRunSpeed = 10;
        public float agentTerminalVel = 20;

        [Header("DASH")]
        public float dashBoostForce = 20f;
        public ForceMode dashForceMode = ForceMode.Impulse;
        public float dashCoolDownDuration = .2f;
        public float dashCoolDownTimer;

        [Header("IDLE")]
        //coefficient used to dampen velocity when idle
        //the purpose of this is to fine tune agent drag
        //...and prevent the agent sliding around while grounded
        //0 means it will instantly stop when grounded
        //1 means no drag will be applied
        public float agentIdleDragVelCoeff = .9f;

        [Header("BODY ROTATION")]
        public float MouseSensitivity = 1;
        public float MouseSmoothTime = 0.05f;
        private float m_Yaw;
        private float m_SmoothYaw;
        private float m_YawSmoothV;
        Quaternion originalRotation;

        [Header("FALLING FORCE")]
        //force applied to agent while falling
        public float agentFallingSpeed = 50f;

        private Rigidbody rb;
        public Animator anim;
        private float inputH;
        private float inputV;
        ArenaAgentInput m_Input;

        private ArenaAgent m_Agent;
        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            m_Agent = GetComponent<ArenaAgent>();
            rb.maxAngularVelocity = maxAngularVel;
            originalRotation = transform.localRotation;
            m_Input = GetComponent<ArenaAgentInput>();
            anim = GetComponent<Animator>();
        }

        public static float ClampAngle(float angle, float min, float max)
        {
            if (angle < -360F)
                angle += 360F;
            if (angle > 360F)
                angle -= 360F;
            return Mathf.Clamp(angle, min, max);
        }

        public void Look(float xRot = 0)
        {
            m_Yaw += xRot * MouseSensitivity;
            float smoothYawOld = m_SmoothYaw;
            m_SmoothYaw = Mathf.SmoothDampAngle(m_SmoothYaw, m_Yaw, ref m_YawSmoothV, MouseSmoothTime);
            rb.MoveRotation(rb.rotation * Quaternion.AngleAxis(Mathf.DeltaAngle(smoothYawOld, m_SmoothYaw), transform.up));
        }

        void FixedUpdate()
        {
            dashCoolDownTimer += Time.fixedDeltaTime;


            if (m_Agent)
            {
                //this disables the heuristic input collection
                m_Agent.disableInputCollectionInHeuristicCallback = allowHumanInputAndDisableAgentHeuristicInput;
            }
            if (!allowHumanInputAndDisableAgentHeuristicInput)
            {
                return;
            }

            float rotate = 0;
            if (!ReferenceEquals(null, m_Input))
            {
                rotate = m_Input.rotateInput;
                inputH = m_Input.moveInput.x;
                inputV = m_Input.moveInput.y;
            }
            var movDir = transform.TransformDirection(new Vector3(inputH, 0, inputV));
            RunOnGround(movDir);
            Look(rotate);

            if (m_Input.CheckIfInputSinceLastFrame(ref m_Input.m_dashPressed))
            {
                Dash(rb.transform.TransformDirection(new Vector3(inputH, 0, inputV)));
            }
            if (m_Agent && m_Input.CheckIfInputSinceLastFrame(ref m_Input.m_attackPressed))
            {
                Attack(m_Agent.AgentHealth.CurrentPercentage); //HEALTH
            }
        }

        public void Dash(Vector3 dir)
        {
            if (dir != Vector3.zero && dashCoolDownTimer > dashCoolDownDuration)
            {
                rb.velocity = Vector3.zero;
                rb.AddForce(dir.normalized * dashBoostForce, dashForceMode);
                dashCoolDownTimer = 0;
            }
        }

        public void RunOnGround(Vector3 dir)
        {
            //ADD FORCE
            var vel = rb.velocity.magnitude;
            float adjustedSpeed = Mathf.Clamp(agentRunSpeed - vel, 0, agentTerminalVel);
            rb.AddForce(dir * adjustedSpeed, runningForceMode);
            anim.SetFloat("Horizontal", dir.x);
            anim.SetFloat("Vertical", dir.y);
        }

        public void Attack(float health)
        {
            if (health <= 50)
            {
                if (!IsAnimationPlaying("Block"))
                {
                    anim.SetTrigger("Block");
                }
            }
            else // Choose between light and heavy attack
            {
                if (!IsAnimationPlaying("HeavyAttack"))
                {
                    anim.SetTrigger("Heavy");
                }
                else if (!IsAnimationPlaying("LightAttack"))
                {
                    anim.SetTrigger("Light");
                }
            }
        }


        public bool IsAnimationPlaying(string animName)
        {
            return anim.GetCurrentAnimatorStateInfo(0).IsName(animName);
        }
    }
}
