using BehaviorDesigner.Runtime;
using UnityEngine;
using BehaviorDesigner.Runtime.Tasks;
using forloopcowboy_unity_tools.Scripts.Soldier;

namespace forloopcowboy_unity_tools.Scripts.BehaviorDesignerTasks
{
    
    [TaskCategory("Custom")]
    public class RefreshActiveWeaponRange : Action
    {
        public SharedFloat rangeOut;
        public SharedGameObject self;

        public override TaskStatus OnUpdate()
        {
            if (self.Value.TryGetComponent(out WeaponUser wpnUser) && wpnUser.TryGetActiveWeapon(out var activeWpn))
            {
                if (activeWpn.weapon != null)
                {
                    rangeOut.SetValue(activeWpn.weapon.weaponSettings.effectiveRange);
                    return TaskStatus.Success;
                }
            }
            
            return TaskStatus.Failure;
        }
    }
}