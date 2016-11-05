using System;
using System.Collections.Generic;
using System.Threading;

enum GeneNodeType { INPUT, OUPUT, HIDDEN };

enum Message { RESET, ITERATE };

namespace GeneticNN
{
    class GeneticNeuralNetwork
    {

        #region Declarations

        public List<Genome> genomePool = new List<Genome>();
        SortedDictionary<double, Genome> rankedGenome = new SortedDictionary<double, Genome>();
        List<List<Genome>> species = new List<List<Genome>>();
        public int generation = 0;
        public int currentGenome = 0;
        public long currentSample = 0;
        Random random = new Random();

        public Message msg = Message.ITERATE;

        #endregion

        public GeneticNeuralNetwork(int input, int output, int no_of_genomes)
        {
            for (int i = 0; i < no_of_genomes; i++)
                genomePool.Add(new Genome(input, output));
        }

        public List<double> Iterate(List<double> input)
        {
            currentSample++;
            if (currentSample > 30 * 60)
                msg = Message.RESET;
            else
                msg = Message.ITERATE;
            return genomePool[currentGenome].Output(input);
        }

        public void UpdatePool(double score)
        {
            score = -score;
            msg = Message.ITERATE;

            genomePool[currentGenome].score = score;

            while (rankedGenome.ContainsKey(score))
                score += 0.00001;

            rankedGenome[score] = genomePool[currentGenome];

            currentGenome++;
            currentSample = 0;


            if (currentGenome == genomePool.Count)
            {
                generation++;
                currentGenome = 0;

                genomePool.Clear();
                foreach (KeyValuePair<double, Genome> itr in rankedGenome)
                    genomePool.Add(itr.Value);

                rankedGenome.Clear();

                #region Speciation

                species.Clear();        // Resets the species matrix

                for (int i = 0; i < genomePool.Count; i++)
                {
                    bool flag = true;
                    for (int j = 0; j < species.Count && flag; j++) // Check if any matching species exists
                        if (MatchGenome(genomePool[i], species[j][0]))
                        {
                            species[j].Add(genomePool[i]);
                            flag = false;
                        }
                    if (flag)
                    {
                        species.Add(new List<Genome>());
                        species[species.Count - 1].Add(genomePool[i]);
                    }
                }

                #endregion

                #region Crossing

                genomePool.Clear();

                for (int i = 0; i < species.Count; i++)
                    Cross(species[i]);

                #endregion

            }
        }

        bool MatchGenome(Genome A, Genome B)
        {
            HashSet<int> h1 = new HashSet<int>();
            HashSet<int> h2 = new HashSet<int>();
            int count1, count2, count3;

            Dictionary<int, double> d1 = new Dictionary<int, double>();
            Dictionary<int, double> d2 = new Dictionary<int, double>();

            for (int i = 0; i < A.connections.Count; i++)
            {
                int refIdx = A.connections[i].innovation_no;
                h1.Add(refIdx);
                d1[refIdx] = A.connections[i].weight;
            }
            for (int i = 0; i < B.connections.Count; i++)
            {
                int refIdx = B.connections[i].innovation_no;
                h2.Add(refIdx);
                d2[refIdx] = B.connections[i].weight;
            }

            count1 = h1.Count;
            count2 = h2.Count;
            h1.IntersectWith(h2);
            count3 = h1.Count;

            double deltaWeight = 0f;

            for (int i = 0; i < A.connections.Count; i++)
                if (h1.Contains(A.connections[i].innovation_no))
                    deltaWeight += Math.Abs(d1[A.connections[i].innovation_no] - d2[A.connections[i].innovation_no]);

            double delta = (double)(count1 + count2 - 2 * count3 + 0.4f * (double)deltaWeight) / (count1 + count2 - count3);
            if (delta < 1.5f)
                return true;
            else
                return false;
        }

        void Cross(List<Genome> genomePool)
        {
            int idx = (int)Math.Ceiling(genomePool.Count * 0.2f);

            if (genomePool.Count == 1)
            {
                genomePool[0].Mutate(true);
                this.genomePool.Add(genomePool[0]);
                return;
            }
            else
                for (int i = 0; i < idx; i++)
                    this.genomePool.Add(genomePool[i]);

            for (int i = idx; i < genomePool.Count; i++)
            {
                int rIdx = random.Next() % idx;
                Genome g1, g2;
                if (genomePool[rIdx].score > genomePool[i].score)
                {
                    g1 = genomePool[rIdx];
                    g2 = genomePool[i];
                }
                else
                {
                    g1 = genomePool[i];
                    g2 = genomePool[rIdx];
                }

                Dictionary<int, int> innvoationIdx = new Dictionary<int, int>();

                for (int j = 0; j < g1.connections.Count; j++)
                    innvoationIdx[g1.connections[j].innovation_no] = j;

                for (int j = 0; j < g2.connections.Count; j++)
                {
                    int idxGene = g2.connections[j].innovation_no;
                    if (innvoationIdx.ContainsKey(idxGene))
                    {
                        int randomVal = random.Next() % 2;
                        if (randomVal == 0)
                            g1.connections[innvoationIdx[idxGene]].weight = g2.connections[j].weight;
                    }
                }

                g1.Mutate(true);

                this.genomePool.Add(g1);
            }
        }

        public void DisplayGenome()
        {
            genomePool[currentGenome].Display();
        }
    }
}

class Genome
{
    #region Declarations

    List<GeneNode> nodes = new List<GeneNode>();
    public List<GeneConnection> connections = new List<GeneConnection>();
    public double score = 0;
    int no_nodes = 0, inputSize, outputSize;
    Random random = new Random();
    double factor = 1f;
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

        int rand_connections = random.Next(15, 30);
        for (int i = 0; i < rand_connections; i++)
        {
            Mutate(true, true);
            Thread.Sleep(1);
        }
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
            if (map.ContainsKey(connections[i].source) == false)
                map[connections[i].source] = new List<GeneConnection>();
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
            outputData.Add(nodesBuffer[i + inputSize]);
        return outputData;
    }

    private void PointMutate()
    {
        for (int itr = 0; itr < 5; itr++)
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
            double deltaWeight = random.Next(-100, 100) / 100f;
            connections[n].UpdateWeight(deltaWeight);
        }
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
            double wt = random.Next(-100, 100) / 100f;
            connections.Add(new GeneConnection(n1, n2, wt));
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
        return;
        if (connections.Count == 0)
            return;
        int n = random.Next() % connections.Count;
        connections[n].FlipStatus();
    }

    public void Mutate(bool flag = false, bool initCall = false)
    {
        for (int itr = 0; itr < 20; itr++)
        {
            flag = true;
            while (flag)
            {
                double randomTmp = random.NextDouble();
                if (initCall)
                {
                    if (randomTmp < 0.50)
                    {
                        LinkMutate();
                        flag = false;
                    }
                    else if (connections.Count > 0)
                    {
                        NodeMutate();
                        flag = false;
                    }
                }
                else
                {
                    if (randomTmp < 1f)
                    {
                        flag = false;
                        PointMutate();
                    }
                    if (randomTmp < 0.80f && connections.Count > 0)
                    {
                        flag = false;
                        LinkMutate();
                    }
                    if (randomTmp < 0.80f && connections.Count > 0)
                    {
                        flag = false;
                        NodeMutate();
                    }
                    if (randomTmp < 0.05f && connections.Count > 0)
                    {
                        flag = false;
                        EnableDisableMutation();
                    }
                }
            }
        }
    }

    public void Display()
    {
        double[][] mat = new double[nodes.Count][];
        for (int i = 0; i < nodes.Count; i++)
            mat[i] = new double[nodes.Count];

        for (int i = 0; i < connections.Count; i++)
            mat[connections[i].source][connections[i].destination] = connections[i].weight;

        for (int i = 0; i < nodes.Count; i++)
        {
            for (int j = 0; j < nodes.Count; j++)
                Console.Write(mat[i][j] + " ");
            Console.WriteLine("");
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