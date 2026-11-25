//через инспектор добавляем в List те части которые надо скрыть для вида от первого лица!!!

using System.Collections.Generic;
using UnityEngine;

public class InvisibleObjectsFromFirstPerson : MonoBehaviour
{
      
    [SerializeField]
    public List<MeshRenderer> HideInFirstView;
    public List<GameObject> HideInThirdView;


    // Update is called once per frame
    public void SwitchFirstView()
    {
        for (int i = 0; i < HideInFirstView.Count; i++)
        {
            if (HideInFirstView[i] != null)
                HideInFirstView[i].enabled = false;
        }
        for (int i = 0; i < HideInThirdView.Count; i++)
        {
            if (HideInThirdView[i] != null)
                HideInThirdView[i].SetActive(true);
        }

    }
    public void SwitchThirdView()
    { 
        for (int i = 0; i < HideInThirdView.Count; i++)
        {
            if (HideInThirdView[i])
                HideInThirdView[i].SetActive(false);
        }
        for (int i = 0; i < HideInFirstView.Count; i++)
        {
            if (HideInFirstView[i])
                HideInFirstView[i].enabled = true;
        }
    }
}
