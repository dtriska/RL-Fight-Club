using UnityEngine;
using UnityEngine.SceneManagement;

//[ExecuteAlways]
public class ArenaAgentInput : MonoBehaviour
{
    public bool DisableInput = false;
    private ArenaInputActions inputActions;
    private ArenaInputActions.PlayerActions actionMap;
    public Vector2 moveInput;
    public bool m_LightAttackInput;
    public bool m_HeavyAttackInput;
    public bool m_blockPressed;
    public bool m_dashPressed;
    public float rotateInput;
    void Awake()
    {
        inputActions = new ArenaInputActions();
        actionMap = inputActions.Player;
    }
    void OnEnable()
    {
        inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }

    public bool CheckIfInputSinceLastFrame(ref bool input)
    {
        if (input)
        {
            input = false;
            return true;
        }
        return false;
    }

    void Update()
    {
        if (inputActions.UI.Restart.triggered)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    void FixedUpdate()
    {
        if (DisableInput)
        {
            return;
        }
        moveInput = actionMap.Walk.ReadValue<Vector2>();
        rotateInput = actionMap.Rotate.ReadValue<float>();
        if (actionMap.LightAttack.triggered)
        {
            m_LightAttackInput = true;
        }
        if (actionMap.HeavyAttack.triggered)
        {
            m_HeavyAttackInput = true;
        }
        if (actionMap.Block.triggered)
        {
            m_blockPressed = true;
        }
        if (actionMap.Dash.triggered)
        {
            m_dashPressed = true;
        }
    }
}
