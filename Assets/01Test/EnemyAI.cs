using UnityEngine;
using TMPro;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    enum State { Patrol, Chase, Attack }
    private State currentState = State.Patrol;

    private NavMeshAgent agent;
    public Transform[] patrolPoints;
    public Transform player;
    private int currentIndex = 0;

    public float chaseRange = 5f;
    public float attackRange = 1.5f;

    public TMP_Text stateText;

    private float attackTImer = 0f;

    // start 보다 먼저 실행
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        GoToNextPoint();
    }

    // 매 프레임마다 실행
    private void Update()
    {
        switch (currentState)
        {
            case State.Patrol: UpdatePatrol(); break;
            case State.Chase: UpdateChase(); break;
            case State.Attack: UpdateAttack(); break;
        }
        stateText.text = $"{currentState}";

    }

    // 순찰
    void UpdatePatrol()
    {
        if (Vector3.Distance(transform.position, player.position) < chaseRange)
        {
            currentState = State.Chase;
            return;
        }

        if (!agent.pathPending && agent.remainingDistance < 0.5f)
            GoToNextPoint();
    }

    // 쫓기
    void UpdateChase()
    {

        agent.SetDestination(player.position);

        float dist = Vector3.Distance(transform.position, player.position);

        if (dist < attackRange)
        {
            currentState = State.Attack;
            return;
        }

        if (dist > chaseRange)
        {
            currentState = State.Patrol;
            GoToNextPoint();
            return;
        }
    }

    // 공격
    void UpdateAttack()
    {

        agent.isStopped = true;

        attackTImer += Time.deltaTime;
        if (attackTImer >= 1f)
        {
            Debug.Log("공격!");
            attackTImer = 0f;
        }


        float dist = Vector3.Distance(transform.position, player.position);

        if (dist > attackRange)
        {
            agent.isStopped = false;
            currentState = State.Chase;
        }
    }

    // 순찰 구현
    void GoToNextPoint()
    {
        if (patrolPoints.Length == 0) return;
        agent.SetDestination(patrolPoints[currentIndex].position);
        currentIndex = (currentIndex + 1) % patrolPoints.Length;
    }
}