namespace Journal.Core.Rendering;

public sealed record DiagramTemplate(string Name, string Summary, IReadOnlyList<string> Tips, string Source);

/// <summary>
/// Starter diagrams offered by the template picker. The same list feeds the syntax help, so every example shown there
/// can be copied or loaded into the editor.
/// </summary>
public static class DiagramTemplates
{
    public static DiagramTemplate Default => All[0];

    public static IReadOnlyList<DiagramTemplate> All { get; } =
    [
        new(
            "Flowchart",
            "Steps and decisions connected by arrows.",
            [
                "The first line sets the direction: TD (top down), LR (left to right), BT or RL.",
                "Node shapes: A[Box], B(Rounded), C([Stadium]), D{Decision}, E((Circle)).",
                "Links: --> arrow, --- line, -.-> dotted, ==> thick. Add text with -->|label|.",
                "Group nodes with: subgraph Title ... end."
            ],
            """
            flowchart TD
                Start([Start]) --> Check{Is it working?}
                Check -->|Yes| Done([Done])
                Check -->|No| Fix[Fix the problem]
                Fix --> Check
            """),
        new(
            "Sequence diagram",
            "Messages passed between participants over time.",
            [
                "Declare participants first to control their order; otherwise they appear as they are used.",
                "->> solid arrow, -->> dashed arrow (usually a reply), -x ends with a cross.",
                "Use Note right of Name: text to annotate.",
                "Wrap steps in loop, alt/else or opt blocks, each closed with end."
            ],
            """
            sequenceDiagram
                participant User
                participant App
                participant Database
                User->>App: Request report
                App->>Database: Query data
                Database-->>App: Rows
                App-->>User: Report
            """),
        new(
            "Class diagram",
            "Classes, their members and how they relate.",
            [
                "Members start with + (public), - (private) or # (protected). A ( ) marks a method.",
                "Relations: <|-- inheritance, *-- composition, o-- aggregation, --> association.",
                "Text after a colon labels the relation; quoted values set the cardinality."
            ],
            """
            classDiagram
                class Participant {
                    +int Id
                    +string LastName
                    +GetPlan() Plan
                }
                class Plan {
                    +int Id
                    +string Name
                }
                Participant "many" --> "1" Plan : enrolled in
            """),
        new(
            "State diagram",
            "States and the transitions between them.",
            [
                "[*] is the start or end point.",
                "Label a transition with a colon: StateA --> StateB : Event.",
                "Nest states with: state Name { ... }."
            ],
            """
            stateDiagram-v2
                [*] --> Draft
                Draft --> Review : Submit
                Review --> Approved : Approve
                Review --> Draft : Request changes
                Approved --> [*]
            """),
        new(
            "Entity relationship",
            "Tables and the relationships between them.",
            [
                "Relationship syntax is left, a line, right: ||--o{ means one to zero-or-many.",
                "Cardinality marks: || exactly one, |o zero or one, }o zero or many, }| one or many.",
                "List attributes inside braces as: type name."
            ],
            """
            erDiagram
                PARTICIPANT ||--o{ ENROLLMENT : has
                PLAN ||--o{ ENROLLMENT : covers
                PARTICIPANT {
                    int Id
                    string LastName
                }
            """),
        new(
            "Gantt chart",
            "Tasks scheduled along a timeline.",
            [
                "dateFormat tells Mermaid how to read the dates you type.",
                "Each task is: Name :status, id, start, length. Status can be done, active or crit.",
                "A start of after otherTaskId chains one task onto another."
            ],
            """
            gantt
                title Release plan
                dateFormat YYYY-MM-DD
                section Build
                Design    :done, design, 2026-10-01, 3d
                Implement :active, impl, after design, 5d
                section Release
                Test      :test, after impl, 3d
            """),
        new(
            "Pie chart",
            "Shares of a whole.",
            [
                "Put a title on the first line with: pie title My title.",
                "Each slice is a quoted label, a colon and a number."
            ],
            """
            pie title Time spent
                "Coding" : 45
                "Reviews" : 25
                "Meetings" : 30
            """),
        new(
            "Mind map",
            "Ideas branching out from a central topic.",
            [
                "Indentation decides the hierarchy, so keep it consistent.",
                "Wrap the root in double parentheses, root((Topic)), for a circle."
            ],
            """
            mindmap
              root((Project))
                Goals
                  Ship version one
                Risks
                  Schedule
            """)
    ];

    public static DiagramTemplate? Find(string name)
    {
        return All.FirstOrDefault(template => string.Equals(template.Name, name, StringComparison.OrdinalIgnoreCase));
    }
}
