using System;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.Spell
{
    [RequireComponent(typeof(LineRenderer))]
    public class PreviewComponent : MonoBehaviour
    {
        private bool _previewOn = false;
        private IPreview _currentPreview;
        private LineRenderer _lineRenderer;
        private PreviewContext _context;

        private void Awake()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            _context = new PreviewContext(_lineRenderer);
        }

        private void Update()
        {
            if (_previewOn) _currentPreview?.Update(ref _context);
            else _currentPreview?.Hide();
        }

        public void SetAndEnable(IPreview newPreview)
        {
            _currentPreview.Dispose();
            _currentPreview = newPreview;
            _previewOn = true;
        }
        
    }

    public struct PreviewContext
    {
        public LineRenderer lineRenderer;

        public PreviewContext(LineRenderer lineRenderer)
        {
            this.lineRenderer = lineRenderer;
        }
    }

    public interface IPreview
    {
        /// <summary>Frame By Frame update function.</summary>
        void Update(ref PreviewContext context);

        /// <summary>Called when preview is switched off.</summary>
        void Dispose();

        /// <summary>Called when preview is not enabled on update loop.</summary>
        void Hide();
    }
}