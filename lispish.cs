using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

public class LispishParser
{
    public enum Symbol {
            WS, ID, INT, REAL, STRING, LITERAL,
            Program, SExpr, Atom, List, Seq
        }
    
    public class Node
    {
        public readonly Symbol Symbol;

        public readonly string Value;

        public readonly List<Node> Children = null;

        public Node(){
            Symbol = Symbol.STRING;
            Value = "Placeholder";
        }

        public Node(Symbol symbol, string value){
            Symbol = symbol;
            Value = value;
        }

        public Node(Symbol symbol, params Node[] children) {
            Symbol = symbol;
            Children = new List<Node>(children);
        }

        public override string ToString() {
            if (Children == null) {
                return this.Value;
            } else {
                return string.Join(' ', Children.Select((n)=>n.ToString()));
            }
        }

        public void Print(string prefix = "", bool token = false)
        {
            if (token) {
                Console.WriteLine($"{prefix+this.Symbol, -20} : {this.Value}");
            } else if (this.Children == null) {
                Console.WriteLine($"{prefix+this.Symbol, -40} {this.Value}");
            } else {
                Console.WriteLine($"{prefix+this.Symbol, -40}");
                foreach (Node child in this.Children) {
                    child.Print(prefix + "  ");
                }
            }
        }
    }

    static public List<Node> Tokenize(String src)
    {
        const string INT_REGEX = @"(?>\+|-)?[0-9]+";
        const string REAL_REGEX = @"(?>\+|-)?[0-9]*\.[0-9]+";
        const string STRING_REGEX = @"""(?>\\""|.)*""";
        const string ID_REGEX = @"[^\s""\(\)]+";
        const string LITERAL_REGEX = @"[\\(\\)]";
        const string ERROR_REGEX = @"[^\s]";

        var expersion = new Regex(@"\G"+
        $"\\s*" +
        $"(?<{Symbol.REAL}>{REAL_REGEX})" +
        $"|(?<{Symbol.INT}>{INT_REGEX})" +
        $"|(?<{Symbol.STRING}>{STRING_REGEX})" +
        $"|(?<{Symbol.ID}>{ID_REGEX})" +
        $"|(?<{Symbol.LITERAL}>{LITERAL_REGEX})" +
        $"|(?<ERROR>{ERROR_REGEX})"
        );

        var tokens = new List<Node>();

        
        foreach(Match match in expersion.Matches(src)) {
            foreach(Group group in match.Groups){
                
                if (group.Name == "0") continue;
                if (group.Success) {
                    if (group.Name == "ERROR"){
                        throw new Exception("Bad Token");
                    }
                    Symbol symbol = Enum.Parse<Symbol>(group.Name);
                    tokens.Add(new Node(symbol, group.Value));
                    break;
                }
            }
        }
        return tokens;
    }

    /*
    <Program> ::= {<SExpr>}
    <SExpr> ::= <Atom> | <List>
    <List> ::= () | ( <Seq> )
    <Seq> ::= <SExpr> <Seq> | <SExpr>
    <Atom> ::= ID | INT | REAL | STRING
    */
    static public Node Parse(List<Node> tokens)
    {
        var pos = 0;
        return parseProgram(tokens, ref pos);
    }

    // <Program> ::= {<SExpr>}
    static public Node parseProgram(List<Node> tokens, ref int pos){
        //Console.WriteLine("parseProgram");
        var children = new List<Node>();
        while (pos < tokens.Count()) {
            //Console.WriteLine("while");
            children.Add(parseSExpr(tokens, ref pos));
        }
        return new Node(Symbol.Program, children.ToArray());
    }

    // <SExpr> ::= <Atom> | <List>
    static public Node parseSExpr(List<Node> tokens, ref int pos){
        //Console.WriteLine("parseSExpr");
        if (tokens[pos].Value == "(") {
            return new Node(Symbol.SExpr, parseList(tokens, ref pos));
        } else {
            return new Node(Symbol.SExpr, parseAtom(tokens, ref pos));
        }
    }

    // <List> ::= () | ( <Seq> )
    static public Node parseList(List<Node> tokens, ref int pos){
        //Console.WriteLine("parseList");
        if (tokens[pos].Value == "(" && tokens[pos+1].Value == ")"){
            //Console.WriteLine("if");
            Node[] nodes = new Node[2];
            nodes[0] = tokens[pos];
            nodes[1] = tokens[pos+1];
            pos += 2;
            return new Node(Symbol.List, nodes);
        } else {
            //Console.WriteLine("else");
            Node[] nodes = new Node[3];
            nodes[0] = tokens[pos];
            pos++;
            nodes[1] = parseSeq(tokens, ref pos);
            nodes[2] = tokens[pos];
            pos++;
            return new Node(Symbol.List, nodes);
        }
    }

    // <Seq> ::= <SExpr> <Seq> | <SExpr>
    static public Node parseSeq(List<Node> tokens, ref int pos){
        //Console.WriteLine("parseSeq");
       
       //Console.WriteLine($"pos:{pos} count:{tokens.Count()}");
        if (tokens[pos+1].Value ==")") {
            return new Node(Symbol.Seq, parseSExpr(tokens, ref pos));
        } else {
            //Console.WriteLine("FUCKING WHY");
            
            Node node1 = parseSExpr(tokens, ref pos);
            
            
            if (tokens[pos].Value == ")") {
                return new Node(Symbol.Seq, node1);
            } else {
                Node node2 = parseSeq(tokens, ref pos);
                Node[] nodes = {node1, node2};
                return new Node(Symbol.Seq, nodes);
            }
        }   
    }

    // <Atom> ::= ID | INT | REAL | STRING
    static public Node parseAtom(List<Node> tokens, ref int pos){
        //Console.WriteLine("parseAtom");
        pos++;
        return new Node(Symbol.Atom, tokens[pos-1]);
    }


    static private void CheckString(string lispcode)
    {
        try
        {
            Console.WriteLine(new String('=', 50));
            Console.Write("Input: ");
            Console.WriteLine(lispcode);
            Console.WriteLine(new String('-', 50));

            List<Node> tokens = Tokenize(lispcode);

            Console.WriteLine("Tokens");
            Console.WriteLine(new String('-', 50));
            foreach (Node node in tokens)
            {
                node.Print("", true);
            }
            Console.WriteLine(new String('-', 50));

            Node parseTree = Parse(tokens);

            Console.WriteLine("Parse Tree");
            Console.WriteLine(new String('-', 50));
            parseTree.Print();
            Console.WriteLine(new String('-', 50));
        }
        catch (Exception)
        {
            Console.WriteLine("Threw an exception on invalid input.");
        }
    }


    public static void Main(string[] args)
    {
        //Here are some strings to test on in 
        //your debugger. You should comment 
        //them out before submitting!

        // CheckString(@"()");
        // CheckString(@"(define foo 3)");
        // CheckString(@"(define foo ""bananas"")");
        // CheckString(@"(define foo ""Say \\""Chease!\\"" "")");
        // CheckString(@"(define foo ""Say \\""Chease!\\)");
        // CheckString(@"(+ 3 4)");      
        // CheckString(@"(+ 3.14 (* 4 7))");
        // CheckString(@"(+ 3.14 (* 4 7)");

        CheckString(Console.In.ReadToEnd());
    }
}

