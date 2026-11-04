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
            """),
        new(
            "User journey",
            "How satisfied a person is at each step of a process.",
            [
                "Each task is: Name: score: actor, actor. The score runs from 1 (poor) to 5 (great).",
                "Group tasks with section Title."
            ],
            """
            journey
                title Submitting an expense report
                section Prepare
                  Gather receipts: 3: Employee
                  Fill in the form: 2: Employee
                section Approve
                  Review the report: 4: Manager
                  Reimburse: 5: Manager, Employee
            """),
        new(
            "Git graph",
            "Commits, branches and merges.",
            [
                "commit adds a commit; add id: \"text\" to label it.",
                "branch name creates a branch, checkout name switches to it, merge name merges it into the current branch."
            ],
            """
            gitGraph
                commit id: "Initial"
                branch feature
                checkout feature
                commit id: "Add feature"
                checkout main
                commit id: "Fix bug"
                merge feature
            """),
        new(
            "Timeline",
            "Events grouped by period.",
            [
                "Each line is: period : event. Add more events to a period with extra : event parts.",
                "Group periods with section Title."
            ],
            """
            timeline
                title Product history
                section Early days
                    2024 : Prototype
                    2025 : First release : Feedback gathered
                section Growth
                    2026 : Version two
            """),
        new(
            "Quadrant chart",
            "Items placed on two axes, split into four quadrants.",
            [
                "Axis labels use: x-axis Low --> High (and y-axis).",
                "Points are: Name: [x, y] with both values between 0 and 1."
            ],
            """
            quadrantChart
                title Effort versus value
                x-axis Low effort --> High effort
                y-axis Low value --> High value
                quadrant-1 Plan
                quadrant-2 Do first
                quadrant-3 Skip
                quadrant-4 Delegate
                Reporting: [0.3, 0.7]
                Migration: [0.8, 0.9]
                Cleanup: [0.2, 0.2]
            """),
        new(
            "XY chart",
            "Bars and lines plotted on x and y axes.",
            [
                "x-axis lists categories in brackets, or give a numeric range.",
                "Add one or more series with bar [values] and line [values]."
            ],
            """
            xychart-beta
                title "Tickets closed"
                x-axis [Jan, Feb, Mar, Apr]
                y-axis "Tickets" 0 --> 50
                bar [12, 30, 25, 41]
                line [12, 30, 25, 41]
            """),
        new(
            "Sankey",
            "Flows between nodes, sized by quantity.",
            [
                "Each line is comma separated: source, target, value.",
                "Leave a blank line after sankey-beta, and give every row all three values."
            ],
            """
            sankey-beta

            Salary,Housing,1200
            Salary,Food,500
            Salary,Savings,800
            Bonus,Savings,300
            """),
        new(
            "Kanban",
            "Cards arranged in columns by status.",
            [
                "A column is a line with an id and a title in brackets: id[Title].",
                "Cards are indented under their column: cardId[Text]."
            ],
            """
            kanban
                todo[To do]
                    task1[Write the specification]
                doing[In progress]
                    task2[Build the importer]
                done[Done]
                    task3[Set up the repository]
            """),
        new(
            "Block diagram",
            "Blocks laid out on a grid, joined by arrows.",
            [
                "columns N sets how many blocks fit on a row.",
                "Link blocks with -->, and give a block more room with id:2."
            ],
            """
            block-beta
                columns 3
                Input Process Output
                Input --> Process
                Process --> Output
                Storage:3
            """),
        new(
            "Architecture",
            "Services, groups and how they connect.",
            [
                "Groups are: group id(icon)[Title]. Services are: service id(icon)[Title] in groupId.",
                "Edges name the side each end attaches to: a:R --> L:b. Built-in icons are cloud, database, disk, internet and server."
            ],
            """
            architecture-beta
                group backend(cloud)[Backend]
                service api(server)[API] in backend
                service db(database)[Database] in backend
                service web(internet)[Website]
                web:R --> L:api
                api:R --> L:db
            """),
        new(
            "Requirement diagram",
            "Requirements, the elements that satisfy them and how they are verified.",
            [
                "Requirements need id, text, risk (low, medium, high) and verifymethod.",
                "Relationships: source - satisfies -> target. Others include contains, derives, refines, verifies and traces."
            ],
            """
            requirementDiagram
                requirement exportRequirement {
                    id: 1
                    text: The app exports a report.
                    risk: medium
                    verifymethod: test
                }
                element exporter {
                    type: module
                }
                exporter - satisfies -> exportRequirement
            """),
        new(
            "C4 context",
            "A system, the people who use it and the systems around it.",
            [
                "Person(id, \"Name\", \"Description\") and System(id, \"Name\", \"Description\"); System_Ext marks an outside system.",
                "Rel(from, to, \"Label\") draws a relationship."
            ],
            """
            C4Context
                title Benefits portal
                Person(member, "Member", "Views their benefits")
                System(portal, "Portal", "Shows enrollment details")
                System_Ext(carrier, "Carrier system", "Holds plan data")
                Rel(member, portal, "Uses")
                Rel(portal, carrier, "Reads plan data")
            """),
        new(
            "Packet diagram",
            "The bit layout of a packet or record.",
            [
                "Each row is a bit range, a colon and a quoted label: 0-15: \"Source port\".",
                "A single bit is just its number: 106: \"Flag\"."
            ],
            """
            packet-beta
                title Message header
                0-15: "Source port"
                16-31: "Destination port"
                32-63: "Sequence number"
            """),
        new(
            "Radar chart",
            "Several measures compared on spokes around a center.",
            [
                "axis lists the spokes; curve draws one polygon per series.",
                "Provide one value for each axis, in order."
            ],
            """
            radar-beta
                title Team skills
                axis design["Design"], code["Code"], test["Testing"], ops["Operations"]
                curve a["Team A"]{4, 3, 5, 2}
                curve b["Team B"]{3, 5, 2, 4}
                max 5
            """),
        new(
            "Treemap",
            "Nested rectangles sized by value.",
            [
                "Indentation sets the nesting. Quoted names with no value are groups.",
                "Leaves are: \"Name\": value."
            ],
            """
            treemap-beta
            "Budget"
                "Salaries"
                    "Engineering": 400
                    "Support": 150
                "Equipment"
                    "Laptops": 90
                    "Servers": 120
            """)
    ];

    public static DiagramTemplate? Find(string name)
    {
        return All.FirstOrDefault(template => string.Equals(template.Name, name, StringComparison.OrdinalIgnoreCase));
    }
}
