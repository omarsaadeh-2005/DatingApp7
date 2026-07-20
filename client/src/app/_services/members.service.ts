import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';
import { Member } from '../_models/member';
import { map, of, take } from 'rxjs';
import { PaginatedResult } from '../_models/pagination';
import { UserParams } from '../_models/userParams';
import { AccountService } from './account.service';
import { User } from '../_models/user';
import { getPaginatedResult, getPaginationHeaders } from './paginationHelper';
import { ToastrService } from 'ngx-toastr';

@Injectable({
  providedIn: 'root'
})
export class MembersService {
  baseUrl = environment.apiUrl;
  members: Member[] = [];
  memberCache = new Map();
  user: User | undefined;
  userParams: UserParams | undefined;

  // REMOVED: private memberService: MembersService (Self-injection bug)
  constructor(
    private http: HttpClient, 
    private accountService: AccountService, 
    private toastr: ToastrService
  ) {
    this.accountService.currentUser$.pipe(take(1)).subscribe({
      next: user => {
        if (user) {
          this.userParams = new UserParams(user);
          this.user = user;
        }
      }
    });
  }

  getUserParams() {
    return this.userParams;
  }

  setUserParams(params: UserParams) {
    this.userParams = params;
  }

  resetUserParams() {
    if (this.user) {
      this.userParams = new UserParams(this.user);
      return this.userParams;
    }
    return;
  }

  getMembers(userParams: UserParams) {
    const response = this.memberCache.get(Object.values(userParams).join('-'));

    if (response) return of(response);

    let params = getPaginationHeaders(userParams.pageNumber, userParams.pageSize);

    params = params.append('minAge', userParams.minAge);
    params = params.append('maxAge', userParams.maxAge);
    params = params.append('gender', userParams.gender);
    params = params.append('orderby', userParams.orderBy);

    return getPaginatedResult<Member[]>(this.baseUrl + 'users', params, this.http).pipe(
      map(response => {
        this.memberCache.set(Object.values(userParams).join('-'), response);
        return response;
      })
    );
  }

  getMember(username: string) {
    const member = [...this.memberCache.values()]
      .reduce((arr, elem) => arr.concat(elem.result), [])
      .find((member: Member) => member.userName === username);

    if (member) return of(member);
    return this.http.get<Member>(this.baseUrl + 'users/' + username);
  }

  updateMember(member: Member) {
    return this.http.put(this.baseUrl + 'users', member).pipe(
      map(() => {
        const index = this.members.indexOf(member);
        this.members[index] = { ...this.members[index], ...member };
      })
    );
  }

  planDate(matchData: any) {
    return this.http.post<Array<{ title: string, description: string, vibe: string }>>(
      this.baseUrl + 'ai/plan-date', matchData
    );
  }

  moderatePhoto(photoUrl: string) {
    return this.http.post<{ isSafe: boolean, hasHumanFace: boolean, reason: string }>(
      this.baseUrl + 'ai/moderate-photo', { photoUrl }
    );
  }

  searchMembersWithAi(query: string, members: Member[]) {
  // Pass a lightweight summary of candidates to Gemini
  const candidatesSummary = members.map(m => ({
    userName: m.userName,
    knownAs: m.knownAs,
    city: m.city,
    country: m.country,
    introduction: m.introduction,
    interests: m.interests,
    lookingFor: m.lookingFor
  }));

  return this.http.post<string[]>(this.baseUrl + 'ai/search-members', {
    query: query,
    candidatesJson: JSON.stringify(candidatesSummary)
  });
}

  setMainPhoto(photoId: number) {
    return this.http.put(this.baseUrl + 'users/set-main-photo/' + photoId, {});
  }

  deletePhoto(photoId: number) {
    return this.http.delete(this.baseUrl + 'users/delete-photo/' + photoId);
  }

  addLike(username: string) {
    return this.http.post(this.baseUrl + 'likes/' + username, {});
  }

  getLikes(predicate: string, pageNumber: number, pageSize: number) {
    let params = getPaginationHeaders(pageNumber, pageSize);

    params = params.append('predicate', predicate);

    return getPaginatedResult<Member[]>(this.baseUrl + 'likes', params, this.http);
  }

  generateBio(keywords: string) {
    return this.http.post<{ bio: string }>(this.baseUrl + 'ai/generate-bio', { keywords });
  }
  
  getIcebreakers(matchData: any) {
    return this.http.post<string[]>(this.baseUrl + 'ai/icebreakers', matchData);
  }

  getCompatibility(matchData: any) {
    return this.http.post<{ score: number, reason: string }>(this.baseUrl + 'ai/compatibility', matchData);
  }
}