using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.Bullet
{
    public class EnergyBulletController : BulletController
    {

        public bool dieOnImpact = true;

        public override void ResetBullet()
        {
            base.ResetBullet();

            // Disable main particle system emission (it glows)
            var ps = GetComponent<ParticleSystem>();
            var emission = ps.emission;
            emission.enabled = true;

            // Disable all children except named "dead-spell" or "impact"
            for (int i = 0; i < transform.childCount; i++)
            {
                var c = transform.GetChild(i);
                c.gameObject.SetActive(c.name != "dead-spell");
            }
        }
        protected override void OnFirstImpact(Collision other)
        {
            
        }

        protected override void OnImpact(Collision other)
        {
            if (!dieOnImpact) return;

            // Disable main particle system emission (it glows)
            var ps = GetComponent<ParticleSystem>();
            var emission = ps.emission;
            emission.enabled = false;

            // Disable all children except named "dead-spell" or "impact"
            for (int i = 0; i < transform.childCount; i++)
            {
                var c = transform.GetChild(i);
                c.gameObject.SetActive(c.name == "dead-spell");
            }

            // reset kill sequence (we don't know which impact will be the last)
            CancelKillSequence();
            InitiateKillSequence(Settings.lifetime);

        }

        protected override void OnFinalImpact(Collision other)
        {
            if (!dieOnImpact) return;

            InitiateKillSequence(Settings.lifetime);
        }

    }
}
