// MADE BY @OYSHOBOY FOR WADALITY GAME
// MANERAI LLC.
// FREE TO USE
// SORRY FOR MESS

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class MenuObject
{
    public string name;
    public GameObject uiObject;
}

public class RadialMenuHandler : MonoBehaviour
{
    public enum MenuState
    {
        Closed,
        Busy,
        Opened
    }

    // starting to implementing inventory
    public List<MenuObject> menuObjects = new List<MenuObject>();

    public MenuState menuState = MenuState.Closed;
    [SerializeField] private bool wasFullyOpened = false;
    [Header("Config")] public GameObject[] menuOptions;
    [SerializeField] private float centerOffset = 7f;
    public bool isMenuToggled = false;
    [SerializeField] private GameObject optionsCart;
    [SerializeField] private GameObject optionsOffset;
    [SerializeField] private AnimationCurve openCurve;
    [SerializeField] private float fullOptionScaleModif = 0.65f;
    [SerializeField] private float menuOpenSpeed = 10f;
    [SerializeField] private float optionScaleSpeed = 20f;
    [SerializeField] private float hideMenuDistance = .3f;
    [SerializeField] private float uiElementScaleFactor = 0.71586f;
    
    public GameObject ouraPrefab;

    [Header("System")]
    public bool spawnOuraObjects;
    public bool hideOuraOnZeroObject;
    [Tooltip("PC Fallback, to demonstrate with mouse")]
    public bool mouseSelection;
    public bool isRightHand = false;
    public int indexChosen = 0;
    private int _selectionLastIndexChoosen = 0;
    private int _lastIndexChoosen = 0;
    private float _timeLapsed = 0;
    public Text debugOutput;


    #region Built-in methods

    private void Start()
    {
        InitializeUiObjects();
        InitializeMenuScales();
    }
    
    private void OnDisable()
    {
        StopAllCoroutines();
    }

    private void ResetAndDestroy()
    {
        indexChosen = 0;
        _selectionLastIndexChoosen = 0;
        _lastIndexChoosen = 0;
        _timeLapsed = 0;

        isMenuToggled = false;
        _timeLapsed = 0;
        menuState = MenuState.Closed;
        ObjectsToggler(true);

        foreach (var menuOption in menuOptions)
        {
            Destroy(menuOption);
        }
    }

    private void MenuToggledHandler(bool obj)
    {
        if (obj)
        {
            indexChosen = 0;
            _lastIndexChoosen = 0;
            _selectionLastIndexChoosen = 0;
        }
    }


    private void Update()
    {
        RadialMenuPositionHandler(isMenuToggled);
        NearestOptionListener();
        HideDistanceHandler();

        PCFallBackMenuToggleHandler();
    }

    private void PCFallBackMenuToggleHandler()
    {
        if (Input.GetKeyDown(KeyCode.Mouse2))
        {
            RadialMenuActivationHandler(isRightHand);
        }
        else if (Input.GetKeyUp(KeyCode.Mouse2))
        {
            RadialMenuDeactivationHandler(isRightHand);
        }

        if (Input.GetKeyDown(KeyCode.Q) && !isRightHand)
        {
            RadialMenuActivationHandler(isRightHand);
        }
        else if (Input.GetKeyUp(KeyCode.Q) && !isRightHand)
        {
            RadialMenuDeactivationHandler(isRightHand);
        }

        if (Input.GetKeyDown(KeyCode.E) && isRightHand)
        {
            RadialMenuActivationHandler(isRightHand);
        }
        else if (Input.GetKeyUp(KeyCode.E) && isRightHand)
        {
            RadialMenuDeactivationHandler(isRightHand);
        }
    }

    #endregion

    private void HideDistanceHandler()
    {
        if (!isMenuToggled || menuState == MenuState.Busy) return;
        var self = SelfTransform();
        var dist = Vector3.Distance(optionsCart.transform.position, self.transform.position);

        if (dist > hideMenuDistance)
        {
            ToggleRadialMenu(false);
            UpdateIndexAndSwitchProjeciles();
        }
    }

    private void NearestOptionListener()
    {
        if (isMenuToggled && menuState != MenuState.Busy)
        {
            var nearestObject = menuOptions[indexChosen];
            var nearestDistance = 100f;
            for (int i = 0; i < menuOptions.Length; i++)
            {
                // change SelfTransform to the any other position, you want to track, for example the hand
                var hand = SelfTransform();
                var handPos = hand.position;

                if (mouseSelection)
                {
                    // mouse position to world position
                    handPos = transform.position;
                    var mousePos = Input.mousePosition;
                    if (Camera.main != null)
                    {
                        mousePos.z = Vector3.Distance(transform.position, Camera.main.transform.position) + .3f;
                        handPos = Camera.main.ScreenToWorldPoint(mousePos);
                    }
                    
                    // draw red debug sphere on mouse position
                    Debug.DrawRay(handPos, Vector3.forward, Color.red, 0.5f);
                    
                }
                
                var currOption = menuOptions[i];
                var dist = Vector3.Distance(handPos, currOption.transform.position);

                if (dist < nearestDistance)
                {
                    nearestDistance = dist;
                    nearestObject = currOption;
                    indexChosen = i;

                    if (debugOutput)
                    {
                        debugOutput.text = $"{menuObjects[indexChosen].name} SELECTED".ToUpper();
                    }
                }
            }

            SelectionIndexChosenHandler();
            for (int i = 0; i < menuOptions.Length; i++)
            {
                var currOption = menuOptions[i];
                if (nearestObject == currOption) continue;
                SetObjectScaleConstantly(currOption.transform, Vector3.one * fullOptionScaleModif);
            }

            var scaleToSet = Vector3.one * (fullOptionScaleModif * 1.2f);
            SetObjectScaleConstantly(nearestObject.transform, scaleToSet);
        }
    }

    private void SelectionIndexChosenHandler()
    {
        if (_selectionLastIndexChoosen.Equals(indexChosen)) return;
        _selectionLastIndexChoosen = indexChosen;
        SelectionHoverVibration();
        
        // SELECT HOVER SOUND
    }

    private void SetupRadialMenuPosition()
    {
        var self = SelfTransform();
        var selfPos = self.transform.position;
        optionsCart.transform.position = selfPos;

        var relativeRot = false;

        var targetPos = transform.position;
        var newDirection = (targetPos - selfPos).normalized;
        
        if(selfPos == targetPos) newDirection = self.forward;
        
        var lookAtRot = Quaternion.LookRotation(newDirection);

        var finalRot = lookAtRot;
        if (relativeRot)
        {
            var newRot = self.transform.rotation;
            newRot *= Quaternion.Euler(0, 180, 0);
            finalRot = Quaternion.Lerp(newRot, lookAtRot, 0.5f);
        }

        optionsCart.transform.rotation = finalRot;
    }

    public Transform SelfTransform()
    {
        return transform;
    }

    private void InitializeUiObjects()
    {
        var newOptions = new List<GameObject>();
        var index = 0;
        foreach (var currObj in menuObjects)
        {
            if (currObj.uiObject == null) continue;

            var parent = optionsOffset.transform;

            var newUiElement = new GameObject($"{currObj.name}-UI-element");
            newUiElement.transform.SetParent(parent);
            newUiElement.transform.localPosition = Vector3.zero;
            newUiElement.transform.localRotation = Quaternion.Euler(new Vector3(0, 90f, 0));

            if (spawnOuraObjects)
            {
                if (hideOuraOnZeroObject && index == 0)
                {
                 // do nothing   
                }
                else
                {
                    InstantiateParentedZeroPos(ouraPrefab, newUiElement.transform);
                }
            }

            var uiWeapon = InstantiateParentedZeroPos(currObj.uiObject, newUiElement.transform);
            uiWeapon.transform.localScale = Vector3.one * uiElementScaleFactor;
            uiWeapon.transform.localEulerAngles = Vector3.zero;
            
            // you can add some custom components, like floater and etc.
            newOptions.Add(newUiElement);
            index++;
        }

        menuOptions = newOptions.ToArray();
    }

    private GameObject InstantiateParentedZeroPos(GameObject objectToInstantiate, Transform parent)
    {
        var newObject = Instantiate(objectToInstantiate, parent);
        newObject.transform.localPosition = Vector3.zero;
        return newObject;
    }

    private void InitializeMenuScales()
    {
        if (menuOptions.Length < 1)
        {
            Debug.LogWarning("No objects added");
            return;
        }

        foreach (var currentOption in menuOptions)
        {
            currentOption.transform.localScale = Vector3.zero;
        }

        ToggleAllMenuElements(false);
    }

    private void SelectionHoverVibration()
    {
        // VIBRATE ON HOVER
    }

    public float quickReleaseTimeDelay = .2f;
    public float lastTimeQuickRelease = -5f;

    private void RadialMenuDeactivationHandler(bool isRightHandCalled)
    {
        if (!isMenuToggled) return;
        if (isRightHandCalled != isRightHand) return;

        var currentTime = Time.time;
        var cooledDown = lastTimeQuickRelease + quickReleaseTimeDelay < currentTime;

        if (menuState == MenuState.Busy)
        {
            SelectionHoverVibration();
            if (cooledDown)
            {
                indexChosen = _selectionLastIndexChoosen == 0 ? menuOptions.Length - 1 : 0;
                _selectionLastIndexChoosen = indexChosen;
                lastTimeQuickRelease = currentTime;
            }
        }

        ToggleRadialMenu(false);

        if (cooledDown) UpdateIndexAndSwitchProjeciles();
    }

    private void UpdateIndexAndSwitchProjeciles()
    {
        if (_lastIndexChoosen == indexChosen) return;
        _lastIndexChoosen = indexChosen;
        UpdateIndexAndSwitchProjeciles();
    }


    private void RadialMenuActivationHandler(bool isRightHandCalled)
    {
        if (isRightHandCalled != isRightHand) return;
        SetupRadialMenuPosition();
        ToggleRadialMenu(true);
    }

    private void ObjectsToggler(bool state)
    {
        foreach (var option in menuOptions)
        {
            option.SetActive(state);
        }
    }

    public void ToggleRadialMenu()
    {
        var upcomingState = !isMenuToggled;
        ToggleRadialMenu(upcomingState);
    }

    private void ToggleRadialMenu(bool state)
    {
        isMenuToggled = state;
        _timeLapsed = 0;
        menuState = MenuState.Busy;
        ObjectsToggler(true);
    }

    private void RadialMenuPositionHandler(bool toggled)
    {
        if (menuOptions.Length < 1) return;

        // ONLY PROCESS WHEN BUSY
        if (menuState != MenuState.Busy) return;

        if (Math.Abs(_timeLapsed - 1f) < 0.01f)
        {
            // FINISHED MENU PROCESSING ( Open/Close )
            if (!isMenuToggled)
            {
                menuState = MenuState.Closed;
                ObjectsToggler(false);
                if (wasFullyOpened)
                {
                    // MENU CLOSED SOUND CAN BE PLAYED HERE
                    wasFullyOpened = false;
                    SelectionHoverVibration();
                    ToggleAllMenuElements(false);
                }
            }
            else
            {
                // MENU OPEN SOUND CAN BE PLAYED HERE
                menuState = MenuState.Opened;
                if (!wasFullyOpened)
                {
                    wasFullyOpened = true;
                    SelectionHoverVibration();
                    ToggleAllMenuElements(true);
                }
            }

            return;
        }

        var lengthWithoutOne = menuOptions.Length > 3 ? menuOptions.Length - 2 : menuOptions.Length - 1;
        var eachObjectAngle = 360 / lengthWithoutOne;
        var offset = centerOffset / 100;

        _timeLapsed += Time.deltaTime * menuOpenSpeed;

        if (_timeLapsed >= .98f)
        {
            _timeLapsed = 1f;
        }

        for (int i = 0; i < menuOptions.Length; i++)
        {
            var currObj = menuOptions[i].transform;
            var modif = 1f / lengthWithoutOne * (lengthWithoutOne + 1 - (i + 1)) + 1;
            if (!toggled)
            {
                MoveObjectToPosition(currObj, Vector3.zero, modif);
                ScaleObject(currObj, Vector3.zero, modif);
                continue;
            }


            ScaleObject(currObj, Vector3.one * fullOptionScaleModif, modif);
            // skip default object
            if (i == 0) continue;

            // skip last object
            if (i == menuOptions.Length - 1)
            {
                // make destination position to be forward 
                var destination = Vector3.forward * offset;
                MoveObjectToPosition(currObj, -destination, modif);
                continue;
            }

            var currAngle = 0 + eachObjectAngle * i;
            var sin = Mathf.Sin(Mathf.Deg2Rad * currAngle);
            var cos = Mathf.Cos(Mathf.Deg2Rad * currAngle);
            var currDir = new Vector3(sin, cos, 0);
            var dest = currDir * offset;
            MoveObjectToPosition(currObj, dest, modif);
        }
    }

    private void ToggleAllMenuElements(bool stateOrder)
    {
        foreach (var uiElement in menuOptions)
        {
            uiElement.SetActive(stateOrder);
        }
    }

    private void MoveObjectToPosition(Transform obj, Vector3 dest, float modif)
    {
        var newPos = Vector3.Lerp(obj.localPosition, dest, currentCurve().Evaluate(_timeLapsed * modif));
        obj.localPosition = newPos;
    }

    private void SetObjectScaleConstantly(Transform obj, Vector3 scale)
    {
        var timeSet = Time.deltaTime * optionScaleSpeed;
        var newScale = Vector3.Lerp(obj.localScale, scale, timeSet);
        obj.localScale = newScale;
    }

    private void ScaleObject(Transform obj, Vector3 scale, float modif)
    {
        var newScale = Vector3.Lerp(obj.localScale, scale, currentCurve().Evaluate(_timeLapsed * modif));
        obj.localScale = newScale;
    }

    private AnimationCurve currentCurve()
    {
        var curve = openCurve;
        return curve;
    }
}