using System;
using UnityEngine;

public abstract class ParameterDrawer
{
    // Override this and return false if you want to customize the override checkbox position,
    // else it'll automatically draw it and put the property content in a horizontal scope.

    /// <summary>
    /// Override this and return <c>false</c> if you want to customize the position of the override
    /// checkbox. If you don't, Unity automatically draws the checkbox and puts the property content in a
    /// horizontal scope.
    /// </summary>
    /// <returns><c>false</c> if the override checkbox position is customized, <c>true</c>
    /// otherwise</returns>
    public virtual bool IsAutoProperty() => true;

    /// <summary>
    /// Draws the parameter in the editor. If the input parameter is invalid you should return
    /// <c>false</c> so that Unity displays the default editor for this parameter.
    /// </summary>
    /// <param name="parameter">The parameter to draw.</param>
    /// <param name="title">The label and tooltip of the parameter.</param>
    /// <returns><c>true</c> if the input parameter is valid, <c>false</c> otherwise in which
    /// case Unity will revert to the default editor for this parameter</returns>
    public abstract bool OnGUI(SerializedDataParameter parameter, GUIContent title);

    public Type type;
}