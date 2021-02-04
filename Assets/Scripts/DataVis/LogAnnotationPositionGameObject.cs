using UnityEngine;
using Sereno.Datasets.Annotation;

namespace Sereno.DataVis
{
    public class LogAnnotationPositionGameObject : MonoBehaviour
    {
        public LogAnnotationPositionInstance Component;

        public void Init(LogAnnotationPositionInstance data)
        {
            Component = data;
            //TODO
        }
    }
}