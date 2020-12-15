using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainScr : MonoBehaviour
{
    public float hexagonWidthRatio;
    public float hexagonWidth;
    public float hexagonHeight;
    public float hexSize; //Size of one side in worldspace
    public GameObject hexagonPrefab;
    public GameObject hexCornerPrefab;
    public GameObject bombPrefab;
    public Color[] tileColors;
    public TileSpace tileSpace;
    public Hex[,] hexList; //List of Hexagon object scripts
    public Vector3[,] tilePositionList; //2d array of tile positions in worldspace
    public List<HexCorner> cornerList;
    public Camera cam;
    public static MainScr instance;
    public GameObject selector; //Selection outline object
    public bool mouseDown; //Is mouse down?
    public bool rotating; //is rotation in progress?
    public Vector3 mouseDownPosition; //Position of last mouse down
    public HexCorner selectedCorner; //Last selected corner
    public AudioSource soundMaster; //Sound Player

    //Audio files
    public AudioClip collideSound;
    public AudioClip rotateSound;
    public AudioClip whooshSound;
    public AudioClip coinSound;
    public AudioClip bassSound;
    public AudioClip alarmSound;
    public AudioClip bombSound;

    public int score;
    public Text scoreText;
    public bool started; //False at start
    public GameObject particlePrefab;
    public GameObject explosionPrefab;
    public Sprite starHexSprite;
    public bool nextIsBomb; //Next hexagon will be bomb if true
    public int bombCounter; //Every time a bomb is spawned, this gets +1
    public List<Hex> bombList; //List of bombs on board
    public Vector3Int[,] undoList; //Colors list for undo
    public int undoScore;
    public int turnNo; //If rotation in progress, shows turning number

    public GameObject gameOverBoard;
    public GameObject undoButton;

    // Start is called before the first frame update
    void Start()
    {
        //Values initialized
        float screenRatio = (float)Screen.width / (float)Screen.height;
        instance = this;
        tileSpace.hexScale = Mathf.Min((0.33f * screenRatio) / 0.5625f, 0.35f); //Hex scale is the localScale of hexagon objects. Differs with screen width
        hexagonWidthRatio = Mathf.Sqrt(3f);
        hexagonWidth = 2.56f * tileSpace.hexScale;
        hexSize = hexagonWidth / 2f;
        hexagonHeight = hexSize * hexagonWidthRatio;
        hexList = new Hex[tileSpace.tileNumberX, tileSpace.tileNumberY];
        undoList = new Vector3Int[tileSpace.tileNumberX, tileSpace.tileNumberY];
        cornerList = new List<HexCorner>();
        tilePositionList = new Vector3[tileSpace.tileNumberX, tileSpace.tileNumberY];
        selector.transform.localScale = new Vector3(tileSpace.hexScale*2f, tileSpace.hexScale*2f, 1f);

        Debug.Log("Initiated");

        //tileTotalOffset is the distance between center of screen and hexagon(0,0)
        Vector3 tileTotalOffset = new Vector3((tileSpace.tileNumberX / 2 -((tileSpace.tileNumberX + 1) % 2)/2f) * -(hexagonWidth/2f + hexSize/2f),
            (hexagonHeight * tileSpace.tileNumberY)/2f - hexagonHeight/2f + tileSpace.tileOffsetY, 0f);

        float delayCounter = 0.02f * tileSpace.tileNumberX * tileSpace.tileNumberY; //total delay of falling hexagons

        //Initial positions, hexagons, colors and corner objects are created here
        for (int y = 0; y < tileSpace.tileNumberY; y++)
        {
            for (int x = 0; x < tileSpace.tileNumberX; x++)
            {
                Vector3 hexPosition;
                if (x % 2 == 1) // Odd-q Layout
                {
                    hexPosition = new Vector3(x * (hexagonWidth / 2f + hexSize / 2f), -y * hexagonHeight, 0f) + tileTotalOffset;
                }
                else
                {
                    hexPosition = new Vector3(x * (hexagonWidth / 2f + hexSize / 2f), hexagonHeight/2f + -y * hexagonHeight, 0f) + tileTotalOffset;
                }
                tilePositionList[x, y] = hexPosition;
                GameObject tmpObj = Instantiate(hexagonPrefab, new Vector3(hexPosition.x, 7f, 0f), Quaternion.identity);
                int rnd = Random.Range(0, tileSpace.totalColors);
                tmpObj.GetComponent<SpriteRenderer>().color = tileColors[rnd];
                tmpObj.transform.localScale = new Vector3(tileSpace.hexScale, tileSpace.hexScale, 1f);
                Hex tmpHex = tmpObj.GetComponent<Hex>();
                tmpHex.tileNumberX = x;
                tmpHex.tileNumberY = y;
                tmpHex.colorNo = rnd;
                //tmpHex.coordinate = new Vector2Int(x, y);
                if (Random.Range(0, 15) == 1)
                {
                    tmpHex.gameObject.GetComponent<SpriteRenderer>().sprite = starHexSprite;
                    tmpHex.isBombStar = 1;
                }
                hexList[x, y] = tmpHex;
                tmpHex.MoveToPosition(delayCounter + x * tileSpace.tileNumberY * 0.03f, 0.4f);
                if (x % 2 == 1)
                {
                    CreateCorner(new Vector2Int(x, y), hexPosition);
                }
                delayCounter -= 0.02f;
            }
        }

        //Check and change any initial explosion
        ShapeBoard();

        //Player interact delay for falling hexagons
        Invoke("StartUnlock", 3.7f); 

    }

    // Update is called once per frame
    void Update()
    {
        //Check for swipe
        if (mouseDown && selectedCorner != null && !rotating && started)
        {
            Vector3 camOffset = new Vector3(0f, 0f, 10f);
            if (Vector3.Distance(mouseDownPosition, Input.mousePosition) > 0.148f * Screen.width)
            {
                //angle between mouse down and current mouse position. Then decide for clockwise or counter clockwise
                float angle = Vector3.SignedAngle(cam.ScreenToWorldPoint(mouseDownPosition) - selectedCorner.transform.position + camOffset, cam.ScreenToWorldPoint(Input.mousePosition) - selectedCorner.transform.position + camOffset, Vector3.forward);
                
                int clockside = 1;
                if (angle < 0f)
                {
                    clockside = -1;
                }
                //Start rotation
                RotateCorner(clockside, 0);
            }
        }
        if (Input.GetMouseButtonDown(0))
        {
            mouseDown = true;
            mouseDownPosition = Input.mousePosition;
        }
        if (Input.GetMouseButtonUp(0))
        {
            mouseDown = false;
            
        }

    }

    public void StartUnlock()
    {
        started = true;
    }

    //Check for explosion, if exist, change a color
    public void ShapeBoard()
    {
        List<HexCorner> tmpList = CornerMathes();
        if (tmpList.Count > 0)
        {
            Debug.Log("Reshaping board..");
            for (int i = 0; i < tmpList.Count; i++)
            {
                Hex tmpHex = hexList[tmpList[i].childHexagons[0].x, tmpList[i].childHexagons[0].y];
                tmpHex.colorNo = (tmpHex.colorNo + 1) % tileSpace.totalColors;
                tmpHex.gameObject.GetComponent<SpriteRenderer>().color = tileColors[tmpHex.colorNo];
            }
            //Re check
            ShapeBoard();
        }
    }

    //Returns the list of matching hexagons
    public List<Vector2Int> MatchHexes()
    {
        List<Vector2Int> resultList = new List<Vector2Int>();
        for (int i = 0; i < cornerList.Count; i++)
        {
            int color1 = hexList[cornerList[i].childHexagons[0].x, cornerList[i].childHexagons[0].y].colorNo;
            int color2 = hexList[cornerList[i].childHexagons[1].x, cornerList[i].childHexagons[1].y].colorNo;
            int color3 = hexList[cornerList[i].childHexagons[2].x, cornerList[i].childHexagons[2].y].colorNo;
            if (color1 == color2 && color1 == color3)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (!resultList.Contains(cornerList[i].childHexagons[j]))
                    {
                        resultList.Add(cornerList[i].childHexagons[j]);
                    }
                }
            }
        }
        return resultList;
    }

    //Returns the list of HexCorners where a match exist
    public List<HexCorner> CornerMathes()
    {
        List<HexCorner> resultList = new List<HexCorner>();
        for (int i = 0; i < cornerList.Count; i++)
        {
            int color1 = hexList[cornerList[i].childHexagons[0].x, cornerList[i].childHexagons[0].y].colorNo;
            int color2 = hexList[cornerList[i].childHexagons[1].x, cornerList[i].childHexagons[1].y].colorNo;
            int color3 = hexList[cornerList[i].childHexagons[2].x, cornerList[i].childHexagons[2].y].colorNo;
            if (color1 == color2 && color1 == color3)
            {

                if (!resultList.Contains(cornerList[i]))
                {
                    resultList.Add(cornerList[i]);
                }
                
            }
        }
        return resultList;
    }

    public void RotateCorner(int clock, int turnNo)
    {
        mouseDown = false;
        rotating = true;
        soundMaster.volume = 0.6f;
        soundMaster.PlayOneShot(rotateSound);
        soundMaster.PlayOneShot(whooshSound);
        

        //Buffer initial hexagons
        Hex hex1 = hexList[selectedCorner.childHexagons[0].x, selectedCorner.childHexagons[0].y];
        Hex hex2 = hexList[selectedCorner.childHexagons[1].x, selectedCorner.childHexagons[1].y];
        Hex hex3 = hexList[selectedCorner.childHexagons[2].x, selectedCorner.childHexagons[2].y];

        //Save undo properties
        undoScore = score;
        for (int x = 0; x < tileSpace.tileNumberX; x++)
        {
            for (int y = 0; y < tileSpace.tileNumberY; y++)
            {
                undoList[x, y].x = hexList[x, y].colorNo;
                undoList[x, y].y = hexList[x, y].isBombStar;
                undoList[x, y].z = hexList[x, y].bombTimer;
            }
        }

        //Before rotating, set parent of hexagons to corner, so they can rotate together
        for (int i = 0; i < 3; i++)
        {
            hexList[selectedCorner.childHexagons[i].x, selectedCorner.childHexagons[i].y].transform.SetParent(selector.transform);
            hexList[selectedCorner.childHexagons[i].x, selectedCorner.childHexagons[i].y].gameObject.GetComponent<SpriteRenderer>().sortingOrder = 1;
        }
            selector.transform.DORotate(selector.transform.localRotation.eulerAngles + new Vector3(0f, 0f, clock * 120f), 0.2f, RotateMode.Fast).SetEase(Ease.Linear)
            .OnComplete(() => RotateEnd(clock, turnNo));
        if (clock == 1)
        {
            hexList[selectedCorner.childHexagons[0].x, selectedCorner.childHexagons[0].y] = hex2;
            hex2.tileNumberX = selectedCorner.childHexagons[0].x;
            hex2.tileNumberY = selectedCorner.childHexagons[0].y;
            hexList[selectedCorner.childHexagons[1].x, selectedCorner.childHexagons[1].y] = hex3;
            hex3.tileNumberX = selectedCorner.childHexagons[1].x;
            hex3.tileNumberY = selectedCorner.childHexagons[1].y;
            hexList[selectedCorner.childHexagons[2].x, selectedCorner.childHexagons[2].y] = hex1;
            hex1.tileNumberX = selectedCorner.childHexagons[2].x;
            hex1.tileNumberY = selectedCorner.childHexagons[2].y;
        }
        else
        {
            hexList[selectedCorner.childHexagons[0].x, selectedCorner.childHexagons[0].y] = hex3;
            hex3.tileNumberX = selectedCorner.childHexagons[0].x;
            hex3.tileNumberY = selectedCorner.childHexagons[0].y;
            hexList[selectedCorner.childHexagons[1].x, selectedCorner.childHexagons[1].y] = hex1;
            hex1.tileNumberX = selectedCorner.childHexagons[1].x;
            hex1.tileNumberY = selectedCorner.childHexagons[1].y;
            hexList[selectedCorner.childHexagons[2].x, selectedCorner.childHexagons[2].y] = hex2;
            hex2.tileNumberX = selectedCorner.childHexagons[2].x;
            hex2.tileNumberY = selectedCorner.childHexagons[2].y;
        }
    }

    public void RotateEnd(int clock, int turnNo)
    {
        turnNo++;
        
        //Reset rotations of hexagons
        for (int i = 0; i < 3; i++)
        {
            hexList[selectedCorner.childHexagons[i].x, selectedCorner.childHexagons[i].y].transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        }

        //If turnNo is 3 , it is the initial state
        if (turnNo < 3)
        {
            List<Vector2Int> tmpList = MatchHexes();

            if (tmpList.Count == 0) //No matches
            {
                RotateCorner(clock, turnNo);
            }
            else
            {
                ExplodeHexes();
                selector.transform.rotation = Quaternion.Euler(0f, 0f, selectedCorner.triangleRotation * 60f);
            }
            
        }
        else //Remove hexagons from corner parentship
        {
            rotating = false;
            
            for (int i = 0; i < 3; i++)
            {
                hexList[selectedCorner.childHexagons[i].x, selectedCorner.childHexagons[i].y].transform.SetParent(null);
                hexList[selectedCorner.childHexagons[i].x, selectedCorner.childHexagons[i].y].gameObject.GetComponent<SpriteRenderer>().sortingOrder = 0;
            }
            selector.transform.rotation = Quaternion.Euler(0f, 0f, selectedCorner.triangleRotation * 60f);
        }
    }

    //When all event end, it is the end of a move/turn
    public void EndTurn()
    {
        turnNo++;
        for (int i = 0; i < bombList.Count; i++)
        {
            bombList[i].EndTurn();
        }

        bool moveAvailable = MoveAvailable();

        if (!moveAvailable) //if no moves left, game is over
        {
            Debug.Log("No moves left");
            GameOver();
        }
    }

    public void ExplodeHexes()
    {
        List<Vector2Int> tmpList = MatchHexes();

        if (tmpList.Count == 0) //No matches
        {
            rotating = false;
            selector.SetActive(true);
            undoButton.SetActive(true);
            EndTurn();
        }

        else //Match exist
        {
            
            soundMaster.volume = 0.6f;
            soundMaster.PlayOneShot(bassSound);
            soundMaster.PlayOneShot(coinSound);
            selector.SetActive(false);
            undoButton.SetActive(false);
            List<Hex> tmpHexList = new List<Hex>(); //Transformed list for coordinated list to Hex objects
            List<Hex> moveHexList = new List<Hex>(); //List for moving hexagons
            tmpList = tmpList.OrderBy(t => t.y).ToList();

            for (int i = 0; i < tmpList.Count; i++)
            {
                tmpHexList.Add(hexList[tmpList[i].x, tmpList[i].y]);
            }

            //Clear parents
            for (int i = 0; i < 3; i++)
            {
                hexList[selectedCorner.childHexagons[i].x, selectedCorner.childHexagons[i].y].transform.SetParent(null);
                hexList[selectedCorner.childHexagons[i].x, selectedCorner.childHexagons[i].y].gameObject.GetComponent<SpriteRenderer>().sortingOrder = 0;
            }

            //Loop to check which hexagons will move, their destinations, if any is destroyed, create new hexagons etc.. 
            for (int i = 0; i < tmpList.Count; i++)
            {
                
                Hex tmpHex;
                if (!nextIsBomb)
                {
                    tmpHex = Instantiate(hexagonPrefab, new Vector3(tilePositionList[tmpList[i].x, 0].x, 7f, 0f), Quaternion.identity).GetComponent<Hex>();
                }
                else
                {
                    nextIsBomb = false;
                    tmpHex = Instantiate(bombPrefab, new Vector3(tilePositionList[tmpList[i].x, 0].x, 7f, 0f), Quaternion.identity).GetComponent<Hex>();
                    tmpHex.isBombStar = 2;
                    bombList.Add(tmpHex);
                }
                
                for (int j = tmpList[i].y; j > -1; j--)
                {

                    if (j > 0)
                    {
                        hexList[tmpList[i].x, j] = hexList[tmpList[i].x, j - 1];
                    }
                    else
                    {
                        hexList[tmpList[i].x, j] = tmpHex;
                        tmpHex.colorNo = Random.Range(0, tileSpace.totalColors);
                        tmpHex.gameObject.GetComponent<SpriteRenderer>().color = tileColors[tmpHex.colorNo];
                        tmpHex.gameObject.transform.localScale = new Vector3(tileSpace.hexScale, tileSpace.hexScale, 1f);
                        if (Random.Range(0, 15) == 1 && tmpHex.isBombStar != 2)
                        {
                            tmpHex.gameObject.GetComponent<SpriteRenderer>().sprite = starHexSprite;
                            tmpHex.isBombStar = 1;
                        }
                        tmpHex.tileNumberX = tmpList[i].x;
                        tmpHex.tileNumberY = 0;
                    }
                    hexList[tmpList[i].x, j].tileNumberY = j;
                    if (!tmpHexList.Contains(hexList[tmpList[i].x, j]))
                    {
                        hexList[tmpList[i].x, j].GetComponent<SpriteRenderer>().sortingOrder = 1;
                        if (!moveHexList.Contains(hexList[tmpList[i].x, j]))
                        {
                            moveHexList.Add(hexList[tmpList[i].x, j]);
                        }
                        
                    }
                    
                }
            }

            //Loop to send move commands to moving hexagons
            float delayCounter = 0f;
            float maxCounter = 0f;
            int lastX = -1;
            moveHexList = moveHexList.OrderByDescending(t => t.tileNumberY).ToList();
            moveHexList = moveHexList.OrderBy(t => t.tileNumberX).ToList();
            
            for (int i = 0; i < moveHexList.Count; i++)
            {
                if (moveHexList[i].tileNumberX != lastX)
                {
                    lastX = moveHexList[i].tileNumberX;
                    delayCounter = 0.08f * (lastX % 2) ;
                }
                moveHexList[i].MoveToPosition(0.35f + delayCounter , 0.15f);
                delayCounter += 0.16f;
                if (delayCounter > maxCounter)
                {
                    maxCounter = delayCounter;
                }
            }

            //wait for end of last move
            Invoke("ExplodeHexes", Mathf.Max(maxCounter) +   0.6f);

            //Create particles, set score and destroy exploded hexagons
            for (int i = 0; i < tmpList.Count; i++)
            {
                score += 5;
                if (tmpHexList[i].isBombStar == 1)
                {
                    score += 15;
                }
                if (tmpHexList[i].isBombStar == 2)
                {
                    score += 15;
                    soundMaster.PlayOneShot(bombSound);
                    Instantiate(explosionPrefab, tmpHexList[i].transform.position, Quaternion.identity);
                    bombList.Remove(tmpHexList[i]);
                }
                if (Mathf.FloorToInt(score / 1000) > bombCounter)
                {
                    bombCounter++;
                    nextIsBomb = true;
                }
                scoreText.text = "Score: " + score;
                GameObject tmpObject = Instantiate(particlePrefab, tmpHexList[i].transform.position, Quaternion.identity);
                tmpObject.GetComponent<ParticleSystem>().startColor = tileColors[tmpHexList[i].colorNo];
                Destroy(tmpHexList[i].gameObject);
            }
        }

    }

    public void UndoMove()
    {
        bombList.Clear();
        for (int x = 0; x < tileSpace.tileNumberX; x++)
        {
            for (int y = 0; y < tileSpace.tileNumberY; y++)
            {
                GameObject tmpObj;
                Destroy(hexList[x, y].gameObject);
                if (undoList[x, y].y != 2)
                {
                    tmpObj = Instantiate(hexagonPrefab, tilePositionList[x, y], Quaternion.identity);
                }
                else
                {
                    tmpObj = Instantiate(bombPrefab, tilePositionList[x, y], Quaternion.identity);
                }
                
                tmpObj.GetComponent<SpriteRenderer>().color = tileColors[undoList[x,y].x];
                tmpObj.transform.localScale = new Vector3(tileSpace.hexScale, tileSpace.hexScale, 1f);
                Hex tmpHex = tmpObj.GetComponent<Hex>();
                tmpHex.tileNumberX = x;
                tmpHex.tileNumberY = y;
                tmpHex.colorNo = undoList[x, y].x;
                //tmpHex.coordinate = new Vector2Int(x, y);
                if (undoList[x, y].y == 1)
                {
                    tmpHex.gameObject.GetComponent<SpriteRenderer>().sprite = starHexSprite;
                    tmpHex.isBombStar = 1;
                }
                if (undoList[x, y].y == 2)
                {
                    //tmpHex.gameObject.GetComponent<SpriteRenderer>().sprite = starHexSprite;
                    tmpHex.isBombStar = 2;
                    tmpHex.bombTimer = undoList[x, y].z;
                    tmpHex.gameObject.GetComponentInChildren<TextMesh>().text = tmpHex.bombTimer.ToString();
                    bombList.Add(tmpHex);
                }
                hexList[x, y] = tmpHex;
            }
        }
        score = undoScore;
        scoreText.text = "Score: " + score;
        selectedCorner = null;
        selector.transform.position = new Vector3(6f, 0f, 0f);
        undoButton.SetActive(false);
    }

    //Simple reload scene
    public void RestartGame()
    {
        //gameOverBoard.SetActive(false);
        SceneManager.LoadScene(0);
    }

    public void GameOver()
    {
        gameOverBoard.SetActive(true);
        selector.SetActive(false);
        undoButton.SetActive(false);
        for (int x = 0; x < tileSpace.tileNumberX; x++)
        {
            for (int y = 0; y < tileSpace.tileNumberY; y++)
            {
                GameObject tmpObject = Instantiate(particlePrefab, hexList[x,y].transform.position, Quaternion.identity);
                tmpObject.GetComponent<ParticleSystem>().startColor = tileColors[hexList[x,y].colorNo];
                Destroy(hexList[x, y].gameObject);
            }
        }
        Debug.LogWarning("Game Over!");
    }

    //Loops to check if any rotation leads to any explosion. If not, no moves left.
    public bool MoveAvailable()
    {
        //bool available = false;
        int[,] tmpList = new int[tileSpace.tileNumberX, tileSpace.tileNumberY];
        for (int x = 0; x < tileSpace.tileNumberX; x++)
        {
            for (int y = 0; y < tileSpace.tileNumberY; y++)
            {
                tmpList[x, y] = hexList[x, y].colorNo;
            }
        }

        for (int i = 0; i < cornerList.Count; i++)
        {
            for (int rot = 0; rot < 3; rot++)
            {
                int t1 = tmpList[cornerList[i].childHexagons[0].x, cornerList[i].childHexagons[0].y];
                int t2 = tmpList[cornerList[i].childHexagons[1].x, cornerList[i].childHexagons[1].y];
                int t3 = tmpList[cornerList[i].childHexagons[2].x, cornerList[i].childHexagons[2].y];

                tmpList[cornerList[i].childHexagons[0].x, cornerList[i].childHexagons[0].y] = t2;
                tmpList[cornerList[i].childHexagons[1].x, cornerList[i].childHexagons[1].y] = t3;
                tmpList[cornerList[i].childHexagons[2].x, cornerList[i].childHexagons[2].y] = t1;
                if (rot != 2)
                {
                    for (int j = 0; j < cornerList.Count; j++)
                    {
                        int color1 = tmpList[cornerList[j].childHexagons[0].x, cornerList[j].childHexagons[0].y];
                        int color2 = tmpList[cornerList[j].childHexagons[1].x, cornerList[j].childHexagons[1].y];
                        int color3 = tmpList[cornerList[j].childHexagons[2].x, cornerList[j].childHexagons[2].y];

                        if (color1 == color2 && color1 == color3)
                        {
                            return true;
                        }
                    }
                }
            }

        }
        return false;
    }

    //Creates corner object based on odd hexagons. 4 corners (left, bottom left, bottom right, right) are created
    public void CreateCorner(Vector2Int hexOrigin, Vector3 hexPosition)
    { 
        Vector3 finalPosition;
        GameObject tmpObject;
        HexCorner tmpCorner;
      

        if (hexOrigin.y < tileSpace.tileNumberY - 1 )
        {
            finalPosition = hexPosition + new Vector3(-hexagonWidth / 2f, 0f, 0f);
            tmpObject = Instantiate(hexCornerPrefab, finalPosition, Quaternion.identity);
            tmpObject.transform.localScale = new Vector3(tileSpace.hexScale / 2f * hexagonWidthRatio, tileSpace.hexScale / 2f * hexagonWidthRatio, 1f);
            tmpCorner = tmpObject.GetComponent<HexCorner>();
            tmpCorner.childHexagons.Add(hexOrigin);
            cornerList.Add(tmpCorner);
            if (hexOrigin.x > 0 && hexOrigin.y < tileSpace.tileNumberY - 1)
            {
                tmpCorner.childHexagons.Add(hexOrigin + new Vector2Int(-1, 1));
            }
            if (hexOrigin.x > 0)
            {
                tmpCorner.childHexagons.Add(hexOrigin + new Vector2Int(-1, 0));
            }
        }

        if (hexOrigin.y < tileSpace.tileNumberY - 1 )
        {
            finalPosition = hexPosition + new Vector3(-hexSize / 2f, -hexagonHeight / 2f, 0f);
            tmpObject = Instantiate(hexCornerPrefab, finalPosition, Quaternion.identity);
            tmpObject.transform.localScale = new Vector3(tileSpace.hexScale / 2f * hexagonWidthRatio, tileSpace.hexScale / 2f * hexagonWidthRatio, 1f);
            tmpObject.transform.localRotation = Quaternion.Euler(0f, 0f, 60f);
            tmpCorner = tmpObject.GetComponent<HexCorner>();
            tmpCorner.childHexagons.Add(hexOrigin);
            cornerList.Add(tmpCorner);
            tmpCorner.triangleRotation = 1;
            if (hexOrigin.y < tileSpace.tileNumberY - 1)
            {
                tmpCorner.childHexagons.Add(hexOrigin + new Vector2Int(0, 1));
            }
            if (hexOrigin.y < tileSpace.tileNumberY - 1)
            {
                tmpCorner.childHexagons.Add(hexOrigin + new Vector2Int(-1, 1));
            }
        }

        if (hexOrigin.y < tileSpace.tileNumberY - 1 && hexOrigin.x < tileSpace.tileNumberX - 1)
        {
            finalPosition = hexPosition + new Vector3(hexSize / 2f, -hexagonHeight / 2f, 0f);
            tmpObject = Instantiate(hexCornerPrefab, finalPosition, Quaternion.identity);
            tmpObject.transform.localScale = new Vector3(tileSpace.hexScale / 2f * hexagonWidthRatio, tileSpace.hexScale / 2f * hexagonWidthRatio, 1f);
            tmpObject.transform.localRotation = Quaternion.Euler(0f, 0f, 120f);
            tmpCorner = tmpObject.GetComponent<HexCorner>();
            cornerList.Add(tmpCorner);
            tmpCorner.childHexagons.Add(hexOrigin);
            if (hexOrigin.x < tileSpace.tileNumberX - 1 && hexOrigin.y < tileSpace.tileNumberY - 1)
            {
                tmpCorner.childHexagons.Add(hexOrigin + new Vector2Int(1, 1));
            }
            if (hexOrigin.y < tileSpace.tileNumberY - 1)
            {
                tmpCorner.childHexagons.Add(hexOrigin + new Vector2Int(0, 1));
            }
        }

        if (hexOrigin.y < tileSpace.tileNumberY - 1 && hexOrigin.x < tileSpace.tileNumberX - 1)
        {
            finalPosition = hexPosition + new Vector3(hexagonWidth / 2f, 0f, 0f);
            tmpObject = Instantiate(hexCornerPrefab, finalPosition, Quaternion.identity);
            tmpObject.transform.localScale = new Vector3(tileSpace.hexScale / 2f * hexagonWidthRatio, tileSpace.hexScale / 2f * hexagonWidthRatio, 1f);
            tmpObject.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);
            tmpCorner = tmpObject.GetComponent<HexCorner>();
            tmpCorner.childHexagons.Add(hexOrigin);
            cornerList.Add(tmpCorner);
            tmpCorner.triangleRotation = 1;
            if (hexOrigin.x < tileSpace.tileNumberX - 1)
            {
                tmpCorner.childHexagons.Add(hexOrigin + new Vector2Int(1, 0));
            }
            if (hexOrigin.x < tileSpace.tileNumberX - 1 && hexOrigin.y < tileSpace.tileNumberY - 1)
            {
                tmpCorner.childHexagons.Add(hexOrigin + new Vector2Int(1, 1));
            }
        }
    }
}
[System.Serializable]
public class TileSpace
{
    public float hexScale;
    public int totalColors;
    public float tileGap;
    public int tileNumberX;
    public int tileNumberY;
    public float tileOffsetY;
}
