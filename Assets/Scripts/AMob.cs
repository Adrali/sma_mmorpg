using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public abstract class AMob : MonoBehaviour, Damageable
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
    private const float AttackCooldown = 1.5f; //Le delai entre chaque attaque

    //Protected
    protected Vector3 spawnpoint = Vector3.zero; //L'endroit ou les mobs spawn, si ils s'eloignent trop de ce point ils cherchent a revenir dessus
    protected Rigidbody target = null; //La cible humaine actuelle du mob, pour savoir si il doit l'aggro ou pas
    protected etat currentEtat; //L'etat actuel pour notre mob
    protected Spawner spawner; //Le spawner qui controle ce mob, utilise pour assurer la mort et respawn du mob

    protected float speed; //La vitesse au sol de notre mob
    protected Vector3 destination; //L'endroit ou notre mob veut aller lorsqu'il roam

    protected float waitTimer = 0f; //Le temps à attendre avant que le mob ne se déplace a nouveau

    protected int pointsDeVie; //Le nombre de points de vie du bestiau
    protected int damages; //La puissance de la bete
    protected float attackTimer = 0f; //Combien de temps depuis la derniere attaque ?

    //Public
    public Rigidbody mobRigidbody; //Le rigidbody du monstre, utilise pour se deplacer et tout
    public LayerMask humanLayer; //Le layer du joueur pour qu'on puisse le detecter
    public Text etatUI; //Le truc qui montre notre etat a tout le monde
    public Text PdVUI; //Pour montrer combien de pv il nous reste

    private void OnEnable()
    {
        currentEtat = etat.Roam; //On commence dans un etat neutre
    }

    //Utilise par le spawner pour placer le mob la ou il veut
    public void SetPosition(Vector3 spawn, Spawner sp)
    {
        spawner = sp;
        mobRigidbody.position = spawn; //On place le mob la ou on est instruit
        spawnpoint = mobRigidbody.position; //Lorsque le mob est active (apres sa mort ou la premiere fois ou il apparait), on stocke sa position actuelle comme point de spawn
        destination = mobRigidbody.position + Random.Range(-7, 7) * Vector3.right + Random.Range(-7, 7) * Vector3.forward;
    }

    //Recuperer la position du spawner pour les quetes
    public Vector3 GetSpawnerPosition() => spawner.GetPosition();

    private void OnTriggerEnter(Collider other)
    {
        //Si un joueur rentre dans notre champ de vision alors qu'on a pas de cible, on va aller l'aggresser
        if (humanLayer == (humanLayer | (1 << other.gameObject.layer)) && target == null)
        {
            target = other.gameObject.GetComponentInParent<Rigidbody>();
            currentEtat = etat.Rush;
        }
    }

    //Si on touche quelque chose d'autre on change de destination vite fait
    private void OnCollisionEnter(Collision collision)
    {
        destination = mobRigidbody.position + Random.Range(-2.5f, 2.5f) * Vector3.right + Random.Range(-2.5f, 2.5f) * Vector3.forward;
    }

    private void FixedUpdate()
    {
        if(attackTimer > 0) attackTimer -= Time.fixedDeltaTime; //On reduit le timer d'attaque en permanence

        etatUI.text = currentEtat.ToString();
        PdVUI.text = pointsDeVie.ToString();

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
                    destination = mobRigidbody.position + Random.Range(-7f, 7f) * Vector3.right + Random.Range(-7f, 7f) * Vector3.forward; //Si on est assez proche de notre destination on en gagne une autre
                    currentEtat = etat.Wait; //Une fois qu'on a atteint la destination, on attend un court instant
                }

                //On se deplace
                mobRigidbody.position += (destination - mobRigidbody.position).normalized * (speed / 3.0f) * Time.fixedDeltaTime; //On cherche à accomplir notre action (on va vers la destination
                break;


            case etat.Wait:
                //On bouge pas
                mobRigidbody.velocity = Vector3.zero;
                //On decremente le timer et on repart si c'est le moment
                waitTimer -= Time.fixedDeltaTime;
                if (waitTimer <= 0) currentEtat = etat.Roam;
                break;

            case etat.Rush:
                //On regarde dans le bon sens
                transform.LookAt(target.position);
                if (Vector3.Distance(target.position, mobRigidbody.position) < 1.4f) currentEtat = etat.Attack; //Si on est assez proche on s'arrete et on tape
                else mobRigidbody.position += (target.position - mobRigidbody.position).normalized * speed * Time.fixedDeltaTime; //Sinon, on avance vers la cible
                break;

            case etat.Attack:
                //On regarde dans le bon sens
                transform.LookAt(target.position);
                if (Vector3.Distance(target.position, mobRigidbody.position) > 1.6f) currentEtat = etat.Rush; //Si la cible s'est eloignee il faut la pourchasser
                else if (attackTimer <= 0f) //Si on a le bon timing, on peut attaquer
                {
                    target.GetComponent<Damageable>().TakeDamage(damages, this);
                    attackTimer = AttackCooldown + Random.Range(-0.2f, 0.2f);
                }
                break;
        }
    }

    public void TakeDamage(int damages, Damageable attacker)
    {
        pointsDeVie -= damages; //On prend des degats
        if (pointsDeVie <= 0) OnDeath(attacker); //Lorsqu'on a plus de vie, on meurt
        else //Sinon, on se tourne vers celui qui nous attaque
        {
            target = attacker.GetRigidbody();
            currentEtat = etat.Attack;
        }
    }

    public void OnDeath(Damageable attacker)
    {
        attacker.OnKill();
        spawner.onMobDeath();
        gameObject.SetActive(false);
    }

    public void OnKill()
    {
        Collider2D potentialTarget = Physics2D.OverlapCircle(transform.position, 7f, humanLayer); //Lorsqu'on tue notre cible, on en cherche une potentiellement nouvelle
        if(potentialTarget != null)
        {
            //Si on trouve une nouvelle cible, on va tout de suite l'aggro
            target = potentialTarget.gameObject.GetComponentInParent<Rigidbody>();
            currentEtat = etat.Rush;
        }
        else
        {
            //Si on a pas de cible en vue on va juste retourner a la niche pour le moment
            destination = spawnpoint;
            target = null;
            currentEtat = etat.Roam;
        }
    }

    public Rigidbody GetRigidbody()
    {
        return gameObject.GetComponent<Rigidbody>();
    }
}
