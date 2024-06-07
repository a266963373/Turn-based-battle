using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using JetBrains.Annotations;

public enum BattleState { START, PLAYERTURN, ENEMYTURN, WIN, LOSE };
public enum BattleActionChoice { UNDECIDED, ATTACK, MOVE, DEFEND, ESCAPE};
public class BattleSystem : MonoBehaviour
{
    BattleState state;

    //TODO: remove player/enemy prefab, and add characterPrefab
    public GameObject playerPrefab;
    public GameObject enemyPrefab;

    public GameObject characterPrefab;

    // BG is BattleGround
    public GameObject playerBG;
    public GameObject enemyBG;

    private Character player;
    private Character enemy;

    //public HUD playerHUD;
    //public HUD enemyHUD;

    public ChoiceHUD choiceHUD;
    public CharacterMaker characterMaker;

    public TextMeshProUGUI centerMessage;

    // flags in Update()
    private bool isWaitingForInput = false;
    private bool isMouseClicked = false; 

    // every turn's flags
    private bool isAllChoicesMade = false;
    private bool isTargetChoiceMade = false;

    BattleActionChoice actionChoice;
    private int currentBattleTurn = 0;
    private int skillChoice = -1;
    private bool isReturnButtonClicked = false;

    // reference
    private Skill currentSkill;
    private Character currentCharacter;
    private string[] currentSkillEffectDescription;

    // temps
    private Queue<string> messageQueue = new Queue<string>();
    private bool isDisplayingMessage = false;
    

    // Start is called before the first frame update
    private void Start()
    {
        state = BattleState.START;
        StartCoroutine(SetupBattle());
    }

    private IEnumerator SetupBattle()   // prepare the characters and enter the battle turns
    {
        SetupButtons();

        player = characterMaker.MakeCharacter("Player", Resources.Load<Sprite>("Sprites/272.38"), playerBG);
        player.FlipX();
        enemy = characterMaker.MakeCharacter("Enemy", Resources.Load<Sprite>("Sprites/89.97"), enemyBG);

        player.UpdateHUD();
        enemy.UpdateHUD();

        // enter battle loops
        int maxBattleTurn = 30;
        currentBattleTurn = 0;
        choiceHUD.DisableAllPanels();
        while (state != BattleState.WIN && state != BattleState.LOSE && currentBattleTurn <= maxBattleTurn)
        {
            yield return StartCoroutine(BattleTurn());
            currentBattleTurn++;
        }
        Debug.Log("Battle ended in " + currentBattleTurn + " turns.");

    }

    private IEnumerator BattleTurn()    // display message, make characters move, trigger animation
    {
        // determine whose turn
        if (state == BattleState.START) { state = BattleState.PLAYERTURN; }
        Debug.Log("Turn " + currentBattleTurn + ": state = " + state);


        //TODO: do messages based on the "currentCharacter"
        currentCharacter = player;

        if (state == BattleState.PLAYERTURN)
        {
            //DisplayMessage("Player's Turn.");
            //yield return StartCoroutine(WaitForMouseClick());
            //StartCoroutine(DisplayMessages("Player's Turn."));
            EnqueueMessages("Player's turn.");
            while (isDisplayingMessage)
            {
                yield return null;
            }

            // reset all input, so no "input cache" that makes the next round's input
            ResetAllInput();

            // make the player make all choices
            while (!isAllChoicesMade)
            {
                // 1. make the first choice
                while (actionChoice == BattleActionChoice.UNDECIDED)
                {
                    yield return null;  // this has to be at top, for flags change after it

                    // 1.1. display action choices
                    if (!choiceHUD.actionPanel.activeSelf) { choiceHUD.DisplayActionPanel(); }

                    // 1.2. Attack Choice
                    if (actionChoice == BattleActionChoice.ATTACK)
                    {
                        // 2. make the second choice
                        while (skillChoice == -1)   // have not make the skill choice
                        {
                            yield return null;

                            // return button
                            if (isReturnButtonClicked)
                            {
                                actionChoice = BattleActionChoice.UNDECIDED;
                                isReturnButtonClicked = false;
                                break;
                            }

                            // display skill choices
                            if (!choiceHUD.skillPanel.activeSelf) { choiceHUD.DisplaySkillPanel(currentCharacter); }

                            //TODO: discuss the target choice

                            isAllChoicesMade = true;
                        }
                        // return button

                        // WaitForClick is in the end, so DisableAllPanels can be called first

                        //TODO: add battle effect

                    }

                    // 1.3. Move Choice

                }
                yield return null;  // this does not have to be at top, for nothing to execute after this

            }

            // do the effects
            // All choices made, do the skill effect
            currentSkillEffectDescription = currentCharacter.UseSkill(skillChoice, enemy);
            choiceHUD.DisableAllPanels();
            EnqueueMessages(currentSkillEffectDescription);
            while (isDisplayingMessage)
            {
                yield return null;
            }

            state = BattleState.ENEMYTURN;
        }
        else // enemy's turn
        {
            EnqueueMessages("Enemy's turn.");
            while (isDisplayingMessage)
            {
                yield return null;
            }

            currentSkillEffectDescription = enemy.UseRandomSkill(player);
            EnqueueMessages(currentSkillEffectDescription);
            while (isDisplayingMessage)
            {
                yield return null;
            }

            state = BattleState.PLAYERTURN;
        }

        // determine end battle
        if (player.isDead) { state = BattleState.LOSE; }
        if (enemy.isDead) { state = BattleState.WIN; }

    }

    //private IEnumerator BattleFirstChoice()
    //{

    //}

    //private IEnumerator BattleSkillOfCharacter(Character character)
    //{
    //    if (state == BattleState.PLAYERTURN)
    //    {
    //        yield return StartCoroutine(PlayerTurnRoutine());
    //    }
    //    else
    //    {
    //        enemy.UseSkill(enemy.skills[0], player);
    //        //DisplayMessage("Enemy attacked player.");
    //        //yield return StartCoroutine(WaitForMouseClick());
    //        //yield return StartCoroutine(DMR("Enemy attacked player."));
    //    }
    //}

    private IEnumerator PlayerTurnRoutine()
    {
        // wait for button pressed input
        isWaitingForInput = true;

        while (isWaitingForInput)
        {
            yield return null;
        }

    }

    private IEnumerator WaitForMouseClick()
    {
        // pull a request to click mouse
        isMouseClicked = false;

        while (!isMouseClicked)
        {
            yield return null;
        }
    }


    private void Update()   // intercepts all inputs
    {
        // intercepting mouse clicking
        if (Input.GetMouseButtonDown(0))
        {
            isMouseClicked = true;
        }

        //playerHUD.UpdateHUD();
        //enemyHUD.UpdateHUD();
        player.UpdateHUD();
        enemy.UpdateHUD();

    }

    //private void DisplayMessage(string message)
    //{
    //    centerMessage.text = message;
    //}

    private void EnqueueMessages(string message)
    {
        messageQueue.Enqueue(message);
        if (!isDisplayingMessage)
        {
            StartCoroutine(DisplayMessages());
        }
    }

    private void EnqueueMessages(string[] messages)
    {
        foreach (var message in messages) {
            messageQueue.Enqueue(message);
        }
        if (!isDisplayingMessage)
        {
            StartCoroutine(DisplayMessages());
        }
    }

    private IEnumerator DisplayMessages()
    {
        isDisplayingMessage = true;

        while (messageQueue.Count > 0)
        {
            centerMessage.text = messageQueue.Dequeue();
            Debug.Log(centerMessage.text);
            yield return WaitForMouseClick();
        }

        isDisplayingMessage = false;
    }


    public void ResetAllInput()
    {
        isAllChoicesMade = false;
        isTargetChoiceMade = false;

        actionChoice = BattleActionChoice.UNDECIDED;
        skillChoice = -1;
        isReturnButtonClicked = false;
}

    private void SetupButtons() // this does not change <On Click()> in Unity inspector.
    {
        //choiceHUD.EnableAllPanels();

        choiceHUD.attackButton.onClick.AddListener(OnAttackButtonClicked);

        for (int i = 0; i < choiceHUD.skillButtonsCount; i++)
        {
            int index = i;  // closure problem happens in lambda functions
            choiceHUD.skillButtons[i].onClick.AddListener(() => OnSkillButtonClicked(index));
        }

        choiceHUD.returnButton.onClick.AddListener(OnReturnButtonClicked);

        //choiceHUD.DisableAllPanels();
    }

    private void OnAttackButtonClicked()
    {
        actionChoice = BattleActionChoice.ATTACK;
    }

    private void OnMoveButtonClicked()
    {
        actionChoice = BattleActionChoice.MOVE;
    }

    private void OnDefendButtonClicked()
    {
        actionChoice = BattleActionChoice.DEFEND;
    }

    private void OnEscapeButtonClicked()
    {
        actionChoice = BattleActionChoice.ESCAPE;
    }

    private void OnSkillButtonClicked(int skillIndex)
    {
        skillChoice = skillIndex;
    }

    private void OnReturnButtonClicked()
    {
        isReturnButtonClicked = true;
    }

    private void StopGame()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }



}
