using System.Collections;
using UnityEngine;

namespace Code.Infrastracture
{
    public interface ICoroutineRunner
    {
        Coroutine StartCoroutine(IEnumerator routine);
    }
}