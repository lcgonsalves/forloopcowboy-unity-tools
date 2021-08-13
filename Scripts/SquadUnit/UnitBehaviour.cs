using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using ForLoopCowboyCommons.EditorHelpers;

// [ExecuteInEditMode]
/// Controls a unit of soldiers. 
public class UnitBehaviour : MonoBehaviour
{
    [Tooltip("List of soldiers in the unit.")]
    public List<Soldier> units = new List<Soldier>();

    public float spacing = 1f;

    public Army armyAssociation;

    public List<Army> enemies = new List<Army>(1);

    // controls

    private Controls controls;

    // debug
    public InputActionReference debugAction;

    // internal state

    private List<SoldierBehaviour> living = new List<SoldierBehaviour>(15); // initialize to 15 i guess

    [SerializeField, ReadOnly]
    private Vector2 cursorPosition = Vector2.zero;

    private void OnEnable()
    {
        if (debugAction)
        {
            debugAction.action.Enable();
            debugAction.action.performed += DebugAction;
        }

        if (armyAssociation && enemies.Contains(armyAssociation)) Debug.LogWarning("Enemies includes self. This might result in unexpected behaviour.");

        controls = new Controls();

        controls.Default.Cursor.Enable();
        controls.Default.Cursor.performed += ctx => cursorPosition = ctx.ReadValue<Vector2>();

    }

    private void OnDisable()
    {
        if (debugAction)
        {
            debugAction.action.performed -= DebugAction;
            debugAction.action.Disable();
        }
    }

    private void Start()
    {

        var offset = 0f;
        // instantiate all units in list spaced out x units in a line
        foreach (var unit in units)
        {

            Vector3 position = transform.TransformPoint(new Vector3(offset, 0, 0));
            var obj = Instantiate(unit.prefab, position, Quaternion.identity);
            var component = obj.GetComponent<SoldierBehaviour>();

            // give weapon
            component.weapon = unit.weapon;

            // set identity
            component.identity = unit;

            // set army association
            component.armyAssociation = armyAssociation;

            // set enemies
            component.enemies = enemies;

            if (component != null) living.Add(component); else Debug.LogError("Unit " + unit.name + "doesn't have a soldier behaviour attached.");
            offset += spacing;
        }

    }

    // Stuff to do when debug action is performed
    private void DebugAction(InputAction.CallbackContext ctx)
    {
        RaycastHit hit;
        Ray r = Camera.main.ScreenPointToRay(cursorPosition);

        if (Physics.Raycast(r, out hit, 1000f))
        {
            foreach (var unit in living)
            {
                unit.WalkTo(hit.point);
            }
        }
    }

}