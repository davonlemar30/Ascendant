using UnityEngine;
using UnityEngine.UI;

namespace Ascendant.CelestialDial
{
    // Placeholder line segments use the same reliable uGUI renderer as the controls.
    public sealed class DialGeometry : MonoBehaviour
    {
        public DialView View;
        public bool Dormant;
        readonly RectTransform[] lines = new RectTransform[60];
        public void Redraw()
        {
            if(lines[0]==null)
                for(int i=0;i<lines.Length;i++)
                {
                    var line=new GameObject("Placeholder line",typeof(RectTransform),typeof(Image));
                    line.transform.SetParent(transform,false);
                    lines[i]=(RectTransform)line.transform;
                    lines[i].anchorMin=lines[i].anchorMax=lines[i].pivot=new Vector2(.5f,.5f);
                    line.GetComponent<Image>().raycastTarget=false;
                }
            for(int i=0;i<48;i++)
            {
                float a=i*Mathf.PI*2/48, b=(i+1)*Mathf.PI*2/48;
                Line(i,new Vector2(Mathf.Cos(a),Mathf.Sin(a))*136,
                    new Vector2(Mathf.Cos(b),Mathf.Sin(b))*136,Dormant ? new Color(.2f,.2f,.22f) : new Color(.38f,.38f,.4f));
            }
            for(int family=0;family<4;family++)
                for(int i=0;i<3;i++)
                {
                    int index=48+family*3+i;
                    bool show=View!=null && View.Lesson!=null && View.Lesson.Kin[family];
                    lines[index].gameObject.SetActive(show);
                    if(show)Line(index,View.SeatPosition(family+4*i)*.77f,View.SeatPosition(family+4*((i+1)%3))*.77f,new Color(.62f,.57f,.53f));
                }
        }
        void Line(int index,Vector2 a,Vector2 b,Color color)
        {
            var line=lines[index];line.anchoredPosition=(a+b)/2;
            line.sizeDelta=new Vector2(Vector2.Distance(a,b),2);
            line.localRotation=Quaternion.Euler(0,0,Mathf.Atan2((b-a).y,(b-a).x)*Mathf.Rad2Deg);
            line.GetComponent<Image>().color=color;
        }
    }
}
