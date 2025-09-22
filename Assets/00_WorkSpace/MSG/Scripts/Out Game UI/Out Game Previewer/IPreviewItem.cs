using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPreviewItem
{
    bool isActiveAndEnabled { get; }
    Transform transform { get; }
    void TryBind();
    void TryUnbind();
}
