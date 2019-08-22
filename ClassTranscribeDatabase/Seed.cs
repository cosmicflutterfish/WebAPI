﻿using ClassTranscribeDatabase.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ClassTranscribeDatabase
{
    public static class Seeder
    {
        public static Boolean IsSeeded = false;
        public static void Seed(CTDbContext _context)
        {
            Console.WriteLine("In Seeder");
            if (IsSeeded)
            {
                Console.WriteLine("Skipping Seeding");
                return;
            }
            _context.Database.EnsureCreated();
            IdentityRole Instructor = new IdentityRole { Name = Globals.ROLE_INSTRUCTOR, Id = "0000", NormalizedName = Globals.ROLE_INSTRUCTOR.ToUpper() };
            IdentityRole Student = new IdentityRole { Name = Globals.ROLE_STUDENT, Id = "0001", NormalizedName = Globals.ROLE_STUDENT.ToUpper() };
            IdentityRole Admin = new IdentityRole { Name = Globals.ROLE_ADMIN, Id = "0002", NormalizedName = Globals.ROLE_ADMIN.ToUpper() };
            IdentityRole UniversityAdmin = new IdentityRole { Name = Globals.ROLE_UNIVERSITY_ADMIN, Id = "0003", NormalizedName = Globals.ROLE_UNIVERSITY_ADMIN.ToUpper() };
            IdentityRole TeachingAssistant = new IdentityRole { Name = Globals.ROLE_TEACHING_ASSISTANT, Id = "0004", NormalizedName = Globals.ROLE_TEACHING_ASSISTANT.ToUpper() };

            List<IdentityRole> roles = new List<IdentityRole> { Instructor, Student, Admin, UniversityAdmin, TeachingAssistant };
            for (int i = 0; i < roles.Count(); i++)
            {
                if (!_context.Roles.IgnoreQueryFilters().Any(r => r.Name == roles[i].Name))
                {
                    _context.Roles.Add(roles[i]);
                }
            }

            University university1 = new University
            {
                // University Id begins with 1
                Id = "1001",
                Name = "University of Illinois at Urbana-Champaign",
                Domain = "illinois.edu"
                // Departments = { department1, department2 }
            };

            University unknownUniversity = new University
            {
                // University Id begins with 1
                Id = "0000",
                Name = "Unknown",
                Domain = "UNK"
            };

            List<University> universities = new List<University> { university1, unknownUniversity };

            foreach (var t in universities)
            {
                if (!_context.Universities.IgnoreQueryFilters().Contains(t))
                {
                    _context.Universities.Add(t);
                }
            }

            ApplicationUser shawn = new ApplicationUser
            {
                Id = "1",
                UserName = "ruihua.sui@gmail.com",
                Email = "ruihua.sui@gmail.com",
                FirstName = "Ruihua",
                LastName = "Sui",
                UniversityId = university1.Id,
                NormalizedEmail = "RUIHUA.SUI@GMAIL.COM",
                NormalizedUserName = "RUIHUA.SUI@GMAIL.COM",
                EmailConfirmed = true,
                LockoutEnabled = false,
                SecurityStamp = Guid.NewGuid().ToString()
            };

            ApplicationUser chirantan = new ApplicationUser
            {
                Id = "2",
                UserName = "mahipal2@illinois.edu",
                Email = "mahipal2@illinois.edu",
                FirstName = "Chirantan",
                LastName = "Mahipal",
                UniversityId = university1.Id,
                NormalizedEmail = "MAHIPAL2@ILLINOIS.EDU",
                NormalizedUserName = "MAHIPAL2@ILLINOIS.EDU",
                EmailConfirmed = true,
                LockoutEnabled = false,
                SecurityStamp = Guid.NewGuid().ToString()
            };

            chirantan.PasswordHash = new PasswordHasher<ApplicationUser>().HashPassword(chirantan, chirantan.Email);
            shawn.PasswordHash = new PasswordHasher<ApplicationUser>().HashPassword(shawn, shawn.Email);

            List<ApplicationUser> users = new List<ApplicationUser> { shawn, chirantan };
            foreach (ApplicationUser user in users)
            {
                if (!_context.Users.IgnoreQueryFilters().Any(u => u.Email == user.Email))
                {
                    _context.Users.Add(user);
                    _context.UserRoles.Add(new IdentityUserRole<string> { RoleId = Instructor.Id, UserId = user.Id });
                    _context.UserRoles.Add(new IdentityUserRole<string> { RoleId = Admin.Id, UserId = user.Id });
                }
            }

            _context.SaveChanges();

            Term term1 = new Term
            {
                // Term Id begins with 0
                Id = "0001",
                Name = "Spring 2019",
                StartDate = new DateTime(2019, 1, 14, 0, 0, 0),
                EndDate = new DateTime(2019, 5, 14, 0, 0, 0),
                // Offerings, University, UniversityId
            };

            Term term2 = new Term
            {
                // Term Id begins with 0
                Id = "0002",
                Name = "Fall 2018",
                StartDate = new DateTime(2018, 8, 27, 0, 0, 0),
                EndDate = new DateTime(2019, 1, 10, 0, 0, 0),
                // Offerings, University, UniversityId
            };

            Term term3 = new Term
            {
                // Term Id begins with 0
                Id = "0003",
                Name = "Spring 2018",
                StartDate = new DateTime(2018, 1, 15, 0, 0, 0),
                EndDate = new DateTime(2018, 5, 14, 0, 0, 0),
                // Offerings, University, UniversityId
            };

            term1.UniversityId = university1.Id;
            term2.UniversityId = university1.Id;
            term3.UniversityId = university1.Id;

            Department department1 = new Department
            {
                // department Id begins with 2
                Id = "2001",
                Name = "Computer Science",
                Acronym = "CS",
                UniversityId = university1.Id
            };

            Department department2 = new Department
            {
                // department Id begins with 2
                Id = "2002",
                Name = "Electrical and Computer Engineering",
                Acronym = "ECE",
                UniversityId = university1.Id
            };

            Course course1 = new Course
            {
                Id = "3001",
                CourseName = "Distributed Systems",
                CourseNumber = "425",
                DepartmentId = department1.Id
            };

            Course course2 = new Course
            {
                Id = "3002",
                CourseName = "Distributed Systems",
                CourseNumber = "428",
                DepartmentId = department2.Id,
                //CourseOfferings
            };

            Course test_course = new Course
            {
                Id = "test_course",
                CourseName = "Test Course",
                CourseNumber = "000",
                DepartmentId = department1.Id
            };

            Offering offering2 = new Offering
            {
                Id = "4002",
                SectionName = "AB",
                TermId = term2.Id,
                AccessType = AccessTypes.Public
            };

            CourseOffering course_offering2 = new CourseOffering
            {
                Id = "9002",
                CourseId = test_course.Id,
                OfferingId = offering2.Id
            };

            List<Term> terms = new List<Term> { term1, term2, term3 };
            List<Department> departments = new List<Department> { department1, department2};
            List<Course> courses = new List<Course> { course1, course2, test_course};
            List<Offering> offerings = new List<Offering> { offering2};
            List<CourseOffering> courseOfferings = new List<CourseOffering> { course_offering2 };

            foreach (var t in terms)
            {
                if (!_context.Terms.IgnoreQueryFilters().Contains(t))
                {
                    _context.Terms.Add(t);
                }
            }

            foreach (var t in departments)
            {
                if (!_context.Departments.IgnoreQueryFilters().Contains(t))
                {
                    _context.Departments.Add(t);
                }
            }

            foreach (var t in courses)
            {
                if (!_context.Courses.IgnoreQueryFilters().Contains(t))
                {
                    _context.Courses.Add(t);
                }
            }

            foreach (var t in offerings)
            {
                if (!_context.Offerings.IgnoreQueryFilters().Contains(t))
                {
                    _context.Offerings.Add(t);
                }
            }

            foreach (var t in courseOfferings)
            {
                if (_context.CourseOfferings.IgnoreQueryFilters().Where(u => u.OfferingId == t.OfferingId && u.CourseId == t.CourseId).Count() == 0)
                {
                    _context.CourseOfferings.Add(t);
                }
            }

            _context.SaveChanges();

            UserOffering userOffering2 = new UserOffering
            {
                OfferingId = offering2.Id,
                ApplicationUserId = users[0].Id,
                IdentityRoleId = Instructor.Id
            };

            UserOffering userOffering6 = new UserOffering
            {
                OfferingId = offering2.Id,
                ApplicationUserId = users[1].Id,
                IdentityRoleId = Instructor.Id
            };

            List<UserOffering> userOfferings = new List<UserOffering> { userOffering2, userOffering6 };

            foreach (var t in userOfferings)
            {
                if (!_context.UserOfferings.IgnoreQueryFilters().Any(u => u.OfferingId == t.OfferingId && u.ApplicationUserId == t.ApplicationUserId && u.IdentityRoleId == t.IdentityRoleId))
                {
                    _context.UserOfferings.Add(t);
                }
            }

            _context.SaveChanges();

            Playlist echoPlaylist = new Playlist
            {
                Id = "echo_sample",
                PlaylistIdentifier = "https://echo360.org/section/9d6e3b31-d3ac-4cfa-b44f-24c1a7c60fd5/public",
                SourceType = SourceType.Echo360,
                Name = "Echo Sample"
            };

            Playlist youtubePlaylist = new Playlist
            {
                Id = "youtube_sample",
                PlaylistIdentifier = "PLLssT5z_DsK8Jk8mpFc_RPzn2obhotfDO",
                SourceType = SourceType.Youtube,
                Name = "Youtube Sample"
            };

            Playlist localPlaylist = new Playlist
            {
                Id = "local_sample",
                SourceType = SourceType.Local,
                Name = "Local Sample"
            };

            List<Playlist> playlists = new List<Playlist> { youtubePlaylist, echoPlaylist, localPlaylist};

            foreach (var t in playlists)
            {
                if (!_context.Playlists.IgnoreQueryFilters().Contains(t))
                {
                    _context.Playlists.Add(t);
                }
            }
            youtubePlaylist.OfferingId = offering2.Id;
            echoPlaylist.OfferingId = offering2.Id;
            localPlaylist.OfferingId = offering2.Id;

            _context.SaveChanges();
            IsSeeded = true;
            Console.WriteLine("Seeded");
        }
    }
}