using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AMob : MonoBehaviour
{
    //Enums
    protected enum etat //Les etats possibles pour notre monstre
    {
        Roam, //Roam signifie que le monstre se déplace juste aléatoirement
        Wait //Après avoir atteint sa destination, on attend peut un court instant
    }

    //Constantes
    private const int LeashRange = 15; //La séparation maximale entre le mob et son lieu de spawn

    //Protected
    protected Vector3 spawnpoint = Vector3.zero; //L'endroit ou les mobs spawn, si ils s'eloignent trop de ce point ils cherchent a revenir dessus
    protected AHuman target = null; //La cible humaine actuelle du mob, pour savoir si il doit l'aggro ou pas
    protected etat currentEtat; //L'etat actuel pour notre mob

    protected float speed; //La vitesse au sol de notre mob
    protected Vector3 destination; //L'endroit où notre mob veut aller lorsqu'il roam

    protected float waitTimer = 0f; //Le temps à attendre avant que le mob ne se déplace à nouveau

    //Public
    public Rigidbody mobRigidbody; //Le rigidbody du monstre, utilise pour se deplacer et tout

    private void OnEnable()
    {
        currentEtat = etat.Roam; //On commence dans un etat neutre
        spawnpoint = mobRigidbody.position; //Lorsque le mob est active (apres sa mort ou la premiere fois ou il apparait), on stocke sa position actuelle comme point de spawn
        destination = mobRigidbody.position + (int)(Random.Range(-7f, 7f)) * Vector3.right + (int)(Random.Range(-7f, 7f)) * Vector3.forward;
    }

    private void FixedUpdate()
    {
        switch (currentEtat)
        {
            case etat.Roam:
                //Les calculs de destination
                if (Vector3.Distance(spawnpoint, mobRigidbody.position) > LeashRange)
                {
                    destination = spawnpoint; //Si on est trop loin du point de spawn on y retourne
                    mobRigidbody.position += (destination - mobRigidbody.position).normalized * (speed / 3.0f) * Time.fixedDeltaTime;
                }
                else if (Vector3.Distance(destination, mobRigidbody.position) < 0.1f)
                {
                    waitTimer = Random.Range(2f, 5f);
                    destination = mobRigidbody.position + (int)(Random.Range(-7f, 7f)) * Vector3.right + (int)(Random.Range(-7f, 7f)) * Vector3.forward; //Si on est assez proche de notre destination on en gagne une autre
                    currentEtat = etat.Wait; //Une fois qu'on a atteint la destination, on attend un court instant
                }
                else mobRigidbody.position += (destination - mobRigidbody.position).normalized * (speed / 3.0f) * Time.fixedDeltaTime; //On cherche à accomplir notre action (on va vers la destination
                break;


            case etat.Wait:
                waitTimer -= Time.fixedDeltaTime;
                if (waitTimer <= 0) currentEtat = etat.Roam;
                break;
        }
    }
}
