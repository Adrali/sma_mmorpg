using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public abstract class AMob : MonoBehaviour
{
    //Enums
    protected enum etat //Les etats possibles pour notre monstre
    {
        Roam, //Roam signifie que le monstre se deplace juste aleatoirement
        Wait, //Apres avoir atteint sa destination, on attend peut un court instant
        Rush, //Lorsque qu'on cherche a atteindre le joueur le plus vite possible
        Attack // Lorsqu'on est assez proche on tape
    }

    //Constantes
    private const int LeashRange = 10; //La separation maximale entre le mob et son lieu de spawn

    //Protected
    protected Vector3 spawnpoint = Vector3.zero; //L'endroit ou les mobs spawn, si ils s'eloignent trop de ce point ils cherchent a revenir dessus
    protected Rigidbody target = null; //La cible humaine actuelle du mob, pour savoir si il doit l'aggro ou pas
    protected etat currentEtat; //L'etat actuel pour notre mob

    protected float speed; //La vitesse au sol de notre mob
    protected Vector3 destination; //L'endroit ou notre mob veut aller lorsqu'il roam

    protected float waitTimer = 0f; //Le temps à attendre avant que le mob ne se déplace a nouveau

    //Public
    public Rigidbody mobRigidbody; //Le rigidbody du monstre, utilise pour se deplacer et tout
    public LayerMask humanLayer; //Le layer du joueur pour qu'on puisse le detecter
    public Text etatUI; //Le truc qui montre notre etat a tout le monde

    private void OnEnable()
    {
        currentEtat = etat.Roam; //On commence dans un etat neutre
        spawnpoint = mobRigidbody.position; //Lorsque le mob est active (apres sa mort ou la premiere fois ou il apparait), on stocke sa position actuelle comme point de spawn
        destination = mobRigidbody.position + Random.Range(-7, 7) * Vector3.right + Random.Range(-7, 7) * Vector3.forward;
    }

    private void OnTriggerEnter(Collider other)
    {
        //Si un joueur rentre dans notre champ de vision alors qu'on a pas de cible, on va aller l'aggresser
        if (humanLayer == (humanLayer | (1 << other.gameObject.layer)) && target == null)
        {
            target = other.gameObject.GetComponentInParent<Rigidbody>();
            currentEtat = etat.Rush;
        }
    }

    private void FixedUpdate()
    {
        etatUI.text = currentEtat.ToString();

        //Retourner a la niche prend la priorite maximale dans notre programme
        if (Vector3.Distance(spawnpoint, mobRigidbody.position) > LeashRange)
        {
            destination = spawnpoint;
            target = null;
            currentEtat = etat.Roam;
        }

        switch (currentEtat)
        {
            case etat.Roam:
                //On regarde dans le bon sens
                transform.LookAt(destination);

                //Si on est a destination, on fait une pause et on gagne une autre destination
                if (Vector3.Distance(destination, mobRigidbody.position) < 0.1f)
                {
                    waitTimer = Random.Range(2, 5);
                    destination = mobRigidbody.position + Random.Range(-7, 7) * Vector3.right + Random.Range(-7, 7) * Vector3.forward; //Si on est assez proche de notre destination on en gagne une autre
                    currentEtat = etat.Wait; //Une fois qu'on a atteint la destination, on attend un court instant
                }

                //On se deplace
                mobRigidbody.position += (destination - mobRigidbody.position).normalized * (speed / 3.0f) * Time.fixedDeltaTime; //On cherche à accomplir notre action (on va vers la destination
                break;


            case etat.Wait:
                //On decremente le timer et on repart si c'est le moment
                waitTimer -= Time.fixedDeltaTime;
                if (waitTimer <= 0) currentEtat = etat.Roam;
                break;

            case etat.Rush:
                //On regarde dans le bon sens
                transform.LookAt(target.position);
                if (Vector3.Distance(target.position, mobRigidbody.position) < 1.5f) currentEtat = etat.Attack; //Si on est assez proche on s'arrete et on tape
                else mobRigidbody.position += (target.position - mobRigidbody.position).normalized * speed * Time.fixedDeltaTime; //Sinon, on avance vers la cible
                break;
        }
    }
}
