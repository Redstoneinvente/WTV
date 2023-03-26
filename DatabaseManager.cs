using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class DatabaseManager : MonoBehaviour
{
    public static DatabaseManager Instance { get; private set; }

    public const string URL = @"https://schoolproject-381517-default-rtdb.asia-southeast1.firebasedatabase.app/";

    public SchoolDatabase schoolDatabase;

    public AuthedUser user;

    public delegate void OnResultReceived(string result);
    public static event OnResultReceived onResultReceived;

    public delegate void OnResultSent(string result);
    public static event OnResultSent onResultSent;

    public delegate void OnAuthed(AuthedUser result);
    public static event OnAuthed onAuthed;

    public delegate void OnAuthedFailed(string result);
    public static event OnAuthedFailed onAuthedFailed;

    //testing
    [SerializeField] string personName;
    [SerializeField] string email;
    [SerializeField] string password;
    [SerializeField] bool isTeacher;

    public bool createDummy;

    private void Awake()
    {
        schoolDatabase = new SchoolDatabase();

        Instance = this;

        onResultReceived += Got;
        onResultSent += Sent;

        onAuthed += Authed;
    }

    private void Start()
    {
        onResultReceived += UpdateLocalSchoolDB;
        StartCoroutine(GetRequest(URL + ".json"));
    }

    private void Update()
    {
        if (createDummy)
        {
            createDummy = false;
            CreateDummyDB();
        }
    }

    [ContextMenu("Create User")]
    public void CreateUser()
    {
        Credentials credentials = new Credentials(personName, email, password, isTeacher);

       CreateUser(credentials);
    }

    public void UpadateSchoolDB()
    {
        StartCoroutine(SendRequest(URL + ".json", JsonUtility.ToJson(schoolDatabase)));
    }

    public void UpdateLocalSchoolDB(string json)
    {
        onResultReceived -= UpdateLocalSchoolDB;
        schoolDatabase = new SchoolDatabase();
        //schoolDatabase = JsonUtility.FromJson<SchoolDatabase>(json);
        JsonUtility.FromJsonOverwrite(json, schoolDatabase);
    } 

    public void ReceivedResults(string data)
    {
        //schoolDatabase = JsonUtility.FromJsonOv<SchoolDatabase>(data);
        JsonUtility.FromJsonOverwrite(data, schoolDatabase);
    }

    public void CreateDummyDB()
    {
        schoolDatabase = new SchoolDatabase();

        Credentials cred = new Credentials("P", "D", "S", true);

        Teacher teacher = new Teacher(cred, true);
        teacher.timeTable = new TimeTable();

        SubjectAndClass t = new SubjectAndClass();
        t.grade = "";
        t.time = "";

        teacher.timeTable.subjectAndClass = new SubjectAndClass[1] { t };

        cred.isTeacher = false;
        AdminStaff adminStaff = new AdminStaff(cred);

        adminStaff.internalMessages = new string[1] { "s" };

        Student student = new Student("d", "d", "d");

        Report report = new Report();
        report.student = new Student[1] { student };
        report.description = "s";
        report.timeDate = DateTime.Now.ToShortDateString();
        report.title = "s";
        report.teacher = teacher;

        teacher.students = new Student[1] { student };

        Replacement re = new Replacement();
        re.teacher = teacher;
        re.timeTable = teacher.timeTable;

        teacher.replacements = new Replacement[1] { re };

        schoolDatabase.adminDB.reports = new Report[1] { report };

        schoolDatabase.adminDB.AddStaff(adminStaff);
        schoolDatabase.teachersDB.AddTeacher(teacher);
        schoolDatabase.studentsDB.AddStudents(student);
    } 

    public void CreateUser(Credentials credentials)
    {
        if (credentials.isTeacher)
        {
            List<Teacher> teachers = new List<Teacher>(schoolDatabase.teachersDB.teachers);

            Teacher teacher = new Teacher(credentials, false);

            teachers.Add(teacher);

            schoolDatabase.teachersDB.teachers = teachers.ToArray();
        }
        else
        {
            List<AdminStaff> adminStaffs = new List<AdminStaff>(schoolDatabase.adminDB.adminStaffs);

            AdminStaff admin = new AdminStaff(credentials);

            adminStaffs.Add(admin);

            schoolDatabase.adminDB.adminStaffs = adminStaffs.ToArray();
        }

        StartCoroutine(SendRequest(URL + ".json", JsonUtility.ToJson(schoolDatabase)));
    }

    public void Login(Credentials credentials)
    {
        user = new AuthedUser();

        if (credentials.isTeacher)
        {
            user.user = schoolDatabase.teachersDB.AuthStaff(credentials);
        }
        else
        {
            user.user = schoolDatabase.adminDB.AuthStaff(credentials);
        }

        if (user.user == default)
        {
            onAuthedFailed("Wrong Credentials");
        }
        else
        {
            if (user.user.personName == "crd" && user.user.credentials.email == "crd")
            {
                user.isCardReaderOnly = true;
            }

            onAuthed(user);
        }
    }

    /// <summary>
    /// Triggers <see cref="OnResultSent"/>
    /// </summary>
    /// <param personName="url"></param>
    /// <param personName="json"></param>
    /// <returns></returns>
    public IEnumerator SendRequest(string url, string json)
    {
        UnityWebRequest request = UnityWebRequest.Put(url, json);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            onResultSent("Sent!");
        }
    }

    void Sent(string msg)
    {
        Debug.Log(msg);
    }

    /// <summary>
    /// Triggers <see cref="OnResultReceived"/>
    /// </summary>
    /// <param personName="url"></param>
    /// <returns></returns>
    public IEnumerator GetRequest(string url)
    {
        UnityWebRequest request = UnityWebRequest.Get(url);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            onResultReceived("Got!");
        }
    }

    void Got(string msg)
    {
        Debug.Log(msg);
    }

    void Authed(AuthedUser auth)
    {
        Debug.Log("Authed!");
    }
}

[System.Serializable]
public class AuthedUser
{
    public Staff user;
    public bool isCardReaderOnly;
}

[System.Serializable]
public class SchoolDatabase
{
    public Students studentsDB;
    public Teachers teachersDB;
    public Admin adminDB;

    public SchoolDatabase()
    {
        studentsDB = new Students();
        teachersDB = new Teachers();
        adminDB = new Admin();
    }
}

[System.Serializable]
public class Students
{
    public Student[] students;

    public Students()
    {
        students = new Student[0];
    }


    public void AddStudents(Student student)
    {
        List<Student> students = new List<Student>(this.students);

        if (!students.Contains(student))
        {
            this.students = new Student[0];
            students.Add(student);

            this.students = students.ToArray();
        }
    }
}

[System.Serializable]
public class Student 
{
    public string personName;
    public string id;
    public string grade;

    public Student(string personName, string id, string grade)
    {
        this.personName = personName;
        this.id = id;   
        this.grade = grade;
    }
}

[System.Serializable]
public class Teachers : UsersDB
{
    public Teacher[] teachers;
    
    public Teachers()
    {
        teachers = new Teacher[0];
    }

    public Teacher AuthStaff(Credentials credentials)
    {
        this.users = teachers;

        return (Teacher)this.GetAuthedUser(credentials);
    }

    public void AddTeacher(Teacher teacher)
    {
        List<Teacher> teachers = new List<Teacher>(this.teachers);

        if (!teachers.Contains(teacher))
        {
            this.teachers = new Teacher[0];
            teachers.Add(teacher);

            this.teachers = teachers.ToArray();
        }
    }
}

[System.Serializable]
public class Teacher : Staff
{
    public bool isHOD;
    public bool isAbsent;

    public Student[] students;
    public TimeTable timeTable;
    public Replacement[] replacements;

    public Teacher(Credentials credentials, bool isHOD) : base(credentials)
    {
        this.isHOD = isHOD;
        isAbsent = false;

        students = new Student[0];
        timeTable = new TimeTable();
        replacements = new Replacement[0];
    }
}

[System.Serializable]
public class Replacement 
{
    public Teacher teacher;
    public TimeTable timeTable;
}

[System.Serializable]
public class TimeTable
{
    public SubjectAndClass[] subjectAndClass;
}

[System.Serializable]
public class SubjectAndClass
{
    public string grade;
    public string time;
}

[System.Serializable]
public class Admin : UsersDB
{
    public AdminStaff[] adminStaffs;
    public Report[] reports;

    public Admin()
    {
        adminStaffs = new AdminStaff[0];
        reports = new Report[0];
    }

    public AdminStaff AuthStaff(Credentials credentials)
    {
        this.users = adminStaffs;

        return (AdminStaff)this.GetAuthedUser(credentials);
    }

    public void AddStaff(AdminStaff staff)
    {
        List<AdminStaff> staffs = new List<AdminStaff>(this.adminStaffs);

        if (!staffs.Contains(staff))
        {
            this.adminStaffs = new AdminStaff[0];
            staffs.Add(staff);

            this.adminStaffs = staffs.ToArray();
        }
    }

}

[System.Serializable]
public class Report
{
    public string title;
    public string description;
    public string timeDate;

    public Teacher teacher;
    public Student[] student;
}

[System.Serializable]
public class AdminStaff : Staff
{
    public string[] internalMessages;

    public AdminStaff(Credentials credentials) : base(credentials)
    {
        internalMessages = new string[0];
    }
}

[System.Serializable]
public class Credentials
{
    public string username;
    public string email;
    public string passwordHashed;
    public bool isTeacher;

    public Credentials(string personName, string email, string passowrdUnHashed, bool isTeacher)
    {
        this.username = personName;
        this.email = email;

        RedsCryptographicEngine.CryptoData cryptoData = new RedsCryptographicEngine.CryptoData();
        cryptoData.CalculateHash(passowrdUnHashed);

        this.passwordHashed = cryptoData.hashedData;

        this.isTeacher = isTeacher;
    }

    public bool VerifyCredentials(string password)
    {
        RedsCryptographicEngine.CryptoData cryptoData = new RedsCryptographicEngine.CryptoData();
        cryptoData.CalculateHash(password);

        return cryptoData.CompareHash(this.passwordHashed);
    }

    public void UpdateEmail(string email)
    {
        this.email = email; 
    }

    public void UpdateUserName(string username)
    {
        this.username = username;
    }
}

[System.Serializable]
public class Staff
{
    public string personName;

    public Credentials credentials;

    public Staff(Credentials credentials)
    {
        this.personName = credentials.username;
        this.credentials = credentials;
    }

    public bool Login(string email, string password)
    {
        return credentials.email == email && credentials.VerifyCredentials(password);
    }
}

[System.Serializable]
public class UsersDB
{
    protected Staff[] users;

    protected Staff GetAuthedUser(Credentials credentials)
    {
        foreach (var user in users)
        {
            if (user.credentials.email == credentials.email && user.credentials.VerifyCredentials(credentials.passwordHashed))
            {
                return user;
            }
        }

        return default;
    }
}