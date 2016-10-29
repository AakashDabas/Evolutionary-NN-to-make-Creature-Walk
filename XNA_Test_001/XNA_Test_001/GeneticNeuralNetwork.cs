using System;
using System.Collections.Generic;

enum GeneNodeType { INPUT, OUPUT, HIDDEN };

namespace GeneticNN
{
    class GeneticNeuralNetwork
    {

        #region Declarations

        List<Genome> genomes = new List<Genome>();

        #endregion
        public GeneticNeuralNetwork(int input, int output, int no_of_genomes)
        {
            for (int i = 0; i < no_of_genomes; i++)
            {
                genomes.Add(new Genome(input, output));
            }
        }
    }
}

class Genome
{
    #region Declarations

    List<GeneNode> nodes = new List<GeneNode>();
    List<GeneConnection> connections = new List<GeneConnection>();
    double score = 0;
    int no_nodes = 0, inputSize, outputSize;
    Random random = new Random();
    double factor = 10f;
    Dictionary<int, HashSet<int>> registeredConnection = new Dictionary<int, HashSet<int>>();

    #endregion

    public Genome(int input, int output)
    {
        for (int i = 0; i < input; i++, no_nodes++)
            nodes.Add(new GeneNode(no_nodes, GeneNodeType.INPUT));
        for (int i = 0; i < output; i++, no_nodes++)
            nodes.Add(new GeneNode(no_nodes, GeneNodeType.OUPUT));
        outputSize = output;
        inputSize = input;
        Init();
        Display();
    }

    private void Display()
    {
        Dictionary<int, List<GeneConnection>> map = new Dictionary<int, List<GeneConnection>>();    // To store the graph
        List<int> buffer = new List<int>();                 // Used for BFS
        bool[] key = new bool[nodes.Count];

        for (int i = 0; i < connections.Count; i++)  // Forms the graph of topology
        {
            if (map.ContainsKey(connections[i].source) == false)
                map[connections[i].source] = new List<GeneConnection>();
            map[connections[i].source].Add(connections[i]);
        }
        for (int i = 0; i < inputSize; i++)
        {
            buffer.Add(i);
            if (map.ContainsKey(i) == false)
                map[i] = new List<GeneConnection>();
        }

        int cnt = 0;
        while (buffer.Count > 0)
        {
                if (map.ContainsKey(buffer[0]))
                {
                    foreach (GeneConnection i in map[buffer[0]])
                        if (key[i.destination] == false)
                        {
                            buffer.Add(i.destination);
                            key[i.destination] = true;
                            cnt++;
                        }
                }
            buffer.RemoveAt(0);
        }

    }

    private void Init()
    {
        int rand_connections = random.Next(5, 20);
        for (int i = 0; i < rand_connections; i++)
            mutate(true, true);
    }

    private double Evaluate(double input, int n)
    {
        return 1.0f / (1 + Math.Exp(input));
    }

    public List<double> Output(List<double> input)
    {
        if (input.Count != inputSize)
            Console.WriteLine("Invalid INPUT Dimension in Neural Net");

        Dictionary<int, List<GeneConnection>> map = new Dictionary<int, List<GeneConnection>>();    // To store the graph
        List<int> buffer = new List<int>();                 // Used for BFS
        double[] nodesBuffer = new double[nodes.Count];  // Stores the output of each node
        bool[] key = new bool[nodes.Count];
        int[] no_of_inputs = new int[nodes.Count];

        for (int i = 0; i < input.Count; i++)    // Initializes the value for input nodes
            nodesBuffer[i] = input[i];

        for (int i = 0; i < connections.Count; i++)  // Forms the graph of topology
        {
            map[connections[i].source].Add(connections[i]);
            no_of_inputs[connections[i].destination]++;
        }

        for (int i = 0; i < inputSize; i++)
        {
            buffer.Add(i);
            if (map.ContainsKey(i) == false)
                map[i] = new List<GeneConnection>();
        }

        while (buffer.Count > 0)
        {
            if (map.ContainsKey(buffer[0]))
            {
                foreach (GeneConnection i in map[buffer[0]])
                {
                    if (key[i.destination] == false)
                    {
                        buffer.Add(i.destination);
                        key[i.destination] = true;
                    }

                    nodesBuffer[buffer[0]] = Evaluate(nodesBuffer[buffer[0]], no_of_inputs[buffer[0]]);

                    if (buffer[0] >= input.Count)
                        nodesBuffer[i.destination] += i.weight * nodesBuffer[buffer[0]];
                }
            }
            buffer.RemoveAt(0);
        }
        List<double> outputData = new List<double>();
        for (int i = 0; i < outputSize; i++)
            outputData[i] = nodesBuffer[i + inputSize];
        return outputData;
    }

    private void PointMutate()
    {
        // Change the weight of a randomly selected connection
        int n = -1;
        bool flag = false;
        for (int i = 0; i < 10; i++)
        {
            n = random.Next() % connections.Count;
            if (connections[n].status == true)
            {
                flag = true;
                break;
            }
        }
        if (flag == false)
            return;
        double deltaWeight = random.Next(-100, 100) / 1000f;
        connections[n].UpdateWeight(deltaWeight);
    }

    private void LinkMutate()
    {
        for (int i = 0; i < 10; i++)
        {
            int n1, n2;
            while (true)
            {
                n1 = random.Next();
                n2 = random.Next();
                n1 %= nodes.Count;
                n2 %= nodes.Count;
                if (nodes[n1].type == GeneNodeType.INPUT && nodes[n2].type == GeneNodeType.INPUT)
                    continue;
                else if (nodes[n1].type == GeneNodeType.OUPUT && nodes[n2].type == GeneNodeType.OUPUT)
                    continue;
                else
                    break;
            }
            if (registeredConnection.ContainsKey(n1) == true &&
                registeredConnection[n1].Contains(n2) == true)
                continue;
            double wt = random.NextDouble() * factor;
            connections.Add(new GeneConnection(n1, n2,  wt));
            if (registeredConnection.ContainsKey(n1) == false)
                registeredConnection.Add(n1, new HashSet<int>());
            registeredConnection[n1].Add(n2);
            break;
        }
    }

    private void NodeMutate()
    {
        //  A -> B
        //    W
        //  A X B
        //  A -> C -> B
        //    1    W

        if (connections.Count == 0)
            return;
        int n = random.Next() % connections.Count;
        connections[n].FlipStatus();    // A X B
        nodes.Add(new GeneNode(no_nodes, GeneNodeType.HIDDEN));
        connections.Add(new GeneConnection(connections[n].source, no_nodes, 1f));    // A -> C
        connections.Add(new GeneConnection(no_nodes, connections[n].destination, connections[n].weight)); // C -> B
        registeredConnection.Add(no_nodes, new HashSet<int>());
        registeredConnection[no_nodes].Add(connections[n].destination);
        registeredConnection[connections[n].source].Add(no_nodes);
        no_nodes++;
    }

    private void EnableDisableMutation()
    {
        if (connections.Count == 0)
            return;
        int n = random.Next() % connections.Count;
        connections[n].FlipStatus();
    }

    public void mutate(bool flag = false, bool initCall = false)
    {
        // Decides wether to mutate or not
        if (random.NextDouble() < 0.05)
            flag = true;
        if (flag)
        {
            while (flag)
            {
                double randomTmp = random.NextDouble();
                if (initCall)
                {
                    if (randomTmp < 0.75)
                    {
                        LinkMutate();
                        flag = false;
                    }
                    else if (connections.Count > 0 && false)
                    {
                        NodeMutate();
                        flag = false;
                    }
                }
                else
                {
                    if (randomTmp < 0.25f)
                    {
                        flag = false;
                        PointMutate();
                    }
                    else if (randomTmp < 0.50f && connections.Count > 0)
                    {
                        flag = false;
                        LinkMutate();
                    }
                    else if (randomTmp < 0.75f && connections.Count > 0)
                    {
                        flag = false;
                        NodeMutate();
                    }
                    else
                    {
                        flag = false;
                        EnableDisableMutation();
                    }
                }
            }
        }
    }
}

class GeneNode
{
    #region Declarations

    public int index { get; }
    public GeneNodeType type { get; }

    #endregion;

    public GeneNode(int index, GeneNodeType type)
    {
        this.index = index;
        this.type = type;
    }
}

class GeneConnection
{
    #region Declarations

    public int innovation_no { get; }       // Global Innovotion No
    public int source { get; }              // Source Node
    public int destination { get; }         // Destination Node
    public double weight;                   // Weight between source and destination nodes
    public bool status;             // Status whether current gene is enabled or disabled
    static public int innovation_Uni = 0;   // Last Innovation No
    Dictionary<int, Dictionary<int, int>> innovationHistory = new Dictionary<int, Dictionary<int, int>>();

    #endregion;

    public GeneConnection(int source, int destination, double weight)
    {
        if (innovationHistory.ContainsKey(source))
        {
            if (innovationHistory.ContainsKey(destination) == false)
                innovationHistory[source].Add(destination, innovation_Uni++);
        }
        else
        {
            innovationHistory.Add(source, new Dictionary<int, int>());
            innovationHistory[source].Add(destination, innovation_Uni++);
        }
        innovation_no = innovationHistory[source][destination];
        this.source = source;
        this.destination = destination;
        this.weight = weight;
        status = true;
    }

    public void UpdateWeight(double deltaWeight)
    {
        weight += deltaWeight;
    }

    public void FlipStatus()
    {
        if (status)
            status = false;
        else
            status = true;
    }
}