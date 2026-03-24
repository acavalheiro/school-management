import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { UserPlus, Users, GraduationCap } from 'lucide-react';
import { useAuth } from '../auth/AuthContext';
import { studentsApi, type StudentDto } from '../api/studentsApi';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';

function getGreeting() {
  const hour = new Date().getHours();
  if (hour < 12) return 'Good morning';
  if (hour < 17) return 'Good afternoon';
  return 'Good evening';
}

function getDisplayName(email: string) {
  const name = email.split('@')[0];
  return name.charAt(0).toUpperCase() + name.slice(1);
}

function StatusBadge({ status }: { status: string }) {
  const isActive = status.toLowerCase() === 'active';
  return (
    <span
      className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium ${
        isActive ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-500'
      }`}
    >
      {status}
    </span>
  );
}

export default function DashboardPage() {
  const { user } = useAuth();
  const navigate = useNavigate();
  const [students, setStudents] = useState<StudentDto[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    studentsApi
      .list()
      .then(setStudents)
      .finally(() => setLoading(false));
  }, []);

  const recent = [...students]
    .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
    .slice(0, 5);

  const displayName = user ? getDisplayName(user.email) : '';

  return (
    <div className="max-w-2xl space-y-8">
      {/* Greeting */}
      <div>
        <h1 className="text-2xl font-semibold text-foreground">
          {getGreeting()}, {displayName}
        </h1>
        <p className="text-sm text-muted-foreground mt-1">
          Here's what's happening at your school today.
        </p>
      </div>

      {/* Quick Actions */}
      <div>
        <p className="text-xs font-medium text-muted-foreground uppercase tracking-wider mb-3">
          Quick Actions
        </p>
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <button
            onClick={() => navigate('/students?action=add')}
            className="flex items-start gap-4 p-5 rounded-xl border bg-card hover:bg-muted/50 transition-colors text-left group"
          >
            <div className="w-10 h-10 rounded-lg bg-primary/10 flex items-center justify-center shrink-0 group-hover:bg-primary/20 transition-colors">
              <UserPlus size={20} className="text-primary" />
            </div>
            <div>
              <p className="font-medium text-foreground">Add Student</p>
              <p className="text-sm text-muted-foreground mt-0.5">Enroll a new student</p>
            </div>
          </button>

          <button
            onClick={() => navigate('/students')}
            className="flex items-start gap-4 p-5 rounded-xl border bg-card hover:bg-muted/50 transition-colors text-left group"
          >
            <div className="w-10 h-10 rounded-lg bg-primary/10 flex items-center justify-center shrink-0 group-hover:bg-primary/20 transition-colors">
              <Users size={20} className="text-primary" />
            </div>
            <div>
              <p className="font-medium text-foreground">View All Students</p>
              <p className="text-sm text-muted-foreground mt-0.5">
                {loading ? '…' : `${students.length} student${students.length !== 1 ? 's' : ''} enrolled`}
              </p>
            </div>
          </button>
        </div>
      </div>

      {/* Recent Students */}
      <div>
        <div className="flex items-center justify-between mb-3">
          <p className="text-xs font-medium text-muted-foreground uppercase tracking-wider">
            Recently Added
          </p>
          {students.length > 0 && (
            <button
              onClick={() => navigate('/students')}
              className="text-xs text-muted-foreground hover:text-foreground transition-colors"
            >
              View all →
            </button>
          )}
        </div>

        <Card>
          <CardContent className="p-0">
            {loading ? (
              <div className="divide-y">
                {[1, 2, 3].map((i) => (
                  <div key={i} className="flex items-center gap-3 px-5 py-4">
                    <div className="w-8 h-8 rounded-full bg-muted animate-pulse shrink-0" />
                    <div className="flex-1 space-y-2">
                      <div className="h-3.5 bg-muted rounded animate-pulse w-36" />
                      <div className="h-3 bg-muted rounded animate-pulse w-48" />
                    </div>
                    <div className="h-5 w-14 bg-muted rounded-full animate-pulse" />
                  </div>
                ))}
              </div>
            ) : recent.length === 0 ? (
              <div className="flex flex-col items-center justify-center py-12 text-center">
                <GraduationCap size={32} className="text-muted-foreground/40 mb-3" />
                <p className="text-sm text-muted-foreground">No students yet.</p>
                <Button
                  variant="outline"
                  size="sm"
                  className="mt-4"
                  onClick={() => navigate('/students?action=add')}
                >
                  Add your first student
                </Button>
              </div>
            ) : (
              <div className="divide-y">
                {recent.map((s) => (
                  <div key={s.id} className="flex items-center gap-3 px-5 py-4">
                    <div className="w-8 h-8 rounded-full bg-muted flex items-center justify-center shrink-0 text-xs font-medium text-muted-foreground">
                      {s.firstName[0]}
                      {s.lastName[0]}
                    </div>
                    <div className="flex-1 min-w-0">
                      <p className="text-sm font-medium text-foreground truncate">
                        {s.firstName} {s.lastName}
                      </p>
                      <p className="text-xs text-muted-foreground truncate">{s.email}</p>
                    </div>
                    <StatusBadge status={s.status} />
                  </div>
                ))}
              </div>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
