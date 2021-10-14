using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    //Constantes
    private const int RespawnTimer = 10;

    //Privates
    private float currentTimer = RespawnTimer;
    private bool loopDone;
    private int index;

    //Publics
    public List<GameObject> troupeau; //La liste des mobs lies a ce spawner

    private void Awake()
    {
        currentTimer += Random.Range(0f, 10f);
        //On instantie chacun de nos mobs dans une zone autour du spawner
        foreach (GameObject mob in troupeau) mob.GetComponent<AMob>().SetPosition(transform.position + Random.Range(-3f, 3f) * Vector3.forward + Random.Range(-3f, 3f) * Vector3.right, this);
    }

    private void FixedUpdate()
    {
        //A intervalle regulier, on fait respawn 1 mob
        if (currentTimer <= 0)
        {
            currentTimer = RespawnTimer; //Reset de timer
            //Initialisation de variables
            loopDone = false;
            index = 0;
            while (!loopDone)
            {
                if (index == troupeau.Count) loopDone = true; //Si on a pas de mobs desactives on a fini
                else if (!troupeau[index].activeSelf) //Si on trouve un mob desactive, on l'active, le place, et considere qu'on a fini
                {
                    troupeau[index].SetActive(true);
                    troupeau[index].GetComponent<AMob>().SetPosition(transform.position + Random.Range(-3f, 3f) * Vector3.forward + Random.Range(-3f, 3f) * Vector3.right, this);
                    loopDone = true;
                }
                index++;
            }
        }
        else currentTimer -= Time.fixedDeltaTime;
    }

    //Lorsqu'un monstre meurt le timer accelere un peu pour compenser
    public void onMobDeath()
    {
        currentTimer--;
    }

    //Retourner la position du spawner, pour les quetes
    public Vector3 GetPosition() => transform.position;
}
