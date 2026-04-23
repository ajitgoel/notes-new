 **Step-by-Step: React CRUD App with JSONPlaceholder** 

**Step 1: Create the React Project** 
Open your terminal and run:
```bash
npx create-react-app posts-app
cd posts-app
```

**Step 2: Understand the API** 
JSONPlaceholder gives you these endpoints for posts:

| Operation     | Method   | URL                                            |
| ------------- | -------- | ---------------------------------------------- |
| Get all posts | `GET`    | `https://jsonplaceholder.typicode.com/posts`   |
| Get one post  | `GET`    | `https://jsonplaceholder.typicode.com/posts/1` |
| Create a post | `POST`   | `https://jsonplaceholder.typicode.com/posts`   |
| Update a post | `PUT`    | `https://jsonplaceholder.typicode.com/posts/1` |
| Delete a post | `DELETE` | `https://jsonplaceholder.typicode.com/posts/1` |
Each post has this shape:

```json
{ "id": 1, "userId": 1, "title": "...", "body": "..." }
```

**Note:** JSONPlaceholder is a _fake_ API — `POST`, `PUT`, and `DELETE` calls return success responses but don’t actually persist changes on the server. Your app will handle state locally so it still feels real.

**Step 3: Create the API Service** 

Create `src/api.js` — this keeps all API calls in one place:

```js
const BASE_URL = "https://jsonplaceholder.typicode.com/posts";

export const api = {
  // GET all posts (limit to 20 for readability)
  getPosts: async () => {
    const res = await fetch(`${BASE_URL}?_limit=20`);
    return res.json();
  },

  // POST — create a new post
  createPost: async (post) => {
    const res = await fetch(BASE_URL, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(post),
    });
    return res.json();
  },

  // PUT — update an existing post
  updatePost: async (id, post) => {
    const res = await fetch(`${BASE_URL}/${id}`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(post),
    });
    return res.json();
  },

  // DELETE — remove a post
  deletePost: async (id) => {
    await fetch(`${BASE_URL}/${id}`, { method: "DELETE" });
  },
};
```

  **Step 4: Create the PostForm Component** 
Create `src/PostForm.js`:

```jsx
import { useState, useEffect } from "react";

function PostForm({ onSubmit, editingPost, onCancel }) {
  const [title, setTitle] = useState("");
  const [body, setBody] = useState("");

  // When editingPost changes, fill the form
  useEffect(() => {
    if (editingPost) {
      setTitle(editingPost.title);
      setBody(editingPost.body);
    } else {
      setTitle("");
      setBody("");
    }
  }, [editingPost]);

  const handleSubmit = (e) => {
    e.preventDefault();
    if (!title.trim() || !body.trim()) return;
    onSubmit({ title, body, userId: 1 });
    setTitle("");
    setBody("");
  };

  return (
    <form onSubmit={handleSubmit}>
      <h2>{editingPost ? "Edit Post" : "New Post"}</h2>
      <input
        placeholder="Title"
        value={title}
        onChange={(e) => setTitle(e.target.value)}
      />
      <textarea
        placeholder="Body"
        value={body}
        onChange={(e) => setBody(e.target.value)}
        rows={4}
      />
      <div>
        <button type="submit">
          {editingPost ? "Update" : "Create"}
        </button>
        {editingPost && (
          <button type="button" onClick={onCancel}>
            Cancel
          </button>
        )}
      </div>
    </form>
  );
}
export default PostForm;
```

  

**Step 5: Create the PostList Component** 

Create `src/PostList.js`:

```jsx hl:1,15,16
function PostList({ posts, onEdit, onDelete }) {
// onEdit, onDelete ARE handleEdit and handleDelete from App.js
  if (posts.length === 0) {
    return <p>No posts yet. Create one above!</p>;
  }
  return (
    <ul>
      {posts.map((post) => (
        <li key={post.id}>
          <div>
            <h3>{post.title}</h3>
            <p>{post.body}</p>
          </div>
          <div>
            <button onClick={() => onEdit(post)}>Edit</button>
            <button onClick={() => onDelete(post.id)}>Delete</button>
          </div>
        </li>
      ))}
    </ul>
  );
}
export default PostList;
```

**Step 6: Wire Everything Together in App.js** 
Replace `src/App.js`:

```jsx hl:43,44
import { useState, useEffect } from "react";
import { api } from "./api";
import PostForm from "./PostForm";
import PostList from "./PostList";
function App() {
  const [posts, setPosts] = useState([]);
  const [editingPost, setEditingPost] = useState(null);
  const [loading, setLoading] = useState(true);
  // 1. FETCH posts on mount
  useEffect(() => {
    api.getPosts().then((data) => {
      setPosts(data);
      setLoading(false);
    });
  }, []);
  // 2. CREATE or UPDATE
  const handleSubmit = async (postData) => {
    if (editingPost) {
      // UPDATE
      const updated = await api.updatePost(editingPost.id, postData);
      setPosts(posts.map((p) => (p.id === editingPost.id ? { ...p, ...updated } : p)));
      setEditingPost(null);
    } else {
      // CREATE — give it a unique local id since JSONPlaceholder always returns id:101
      const created = await api.createPost(postData);
      setPosts([{ ...created, id: Date.now() }, ...posts]);
    }
  };
  // 3. DELETE
  const handleDelete = async (id) => {
    await api.deletePost(id);
    setPosts(posts.filter((p) => p.id !== id));
  };
  // 4. EDIT — set the post being edited
  const handleEdit = (post) => {
    setEditingPost(post);
    window.scrollTo({ top: 0, behavior: "smooth" });
  };
  if (loading) return <p>Loading posts...</p>;
  return (
    <div>
      <h1>Posts Manager</h1>
      <PostForm onSubmit={handleSubmit} editingPost={editingPost} onCancel={() => setEditingPost(null)}/>
      <PostList posts={posts} onEdit={handleEdit} onDelete={handleDelete} />
    </div>
  );
}
export default App;
```

  

**Step 7: Run It** 

```bash
npm start
```

  

Your browser opens to `http://localhost:3000`. You should see 20 posts loaded from the API, with a form at the top to create or edit.

**How the CRUD Operations Work** 

|Action|What happens in the UI|API call|
|---|---|---|
|**Page loads**|20 posts appear|`GET /posts?_limit=20`|
|**Click “Create”**|New post appears at top of list|`POST /posts`|
|**Click “Edit”**|Form fills with post data; submit updates it|`PUT /posts/:id`|
|**Click “Delete”**|Post disappears from the list|`DELETE /posts/:id`|
  --------------
  
  ##### `src/api.js`

```js
const BASE_URL = "https://jsonplaceholder.typicode.com/posts";

export const api = {
  getPosts: async () => {
    const res = await fetch(`${BASE_URL}?_limit=20`);
    return res.json();
  },

  createPost: async (post) => {
    const res = await fetch(BASE_URL, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(post),
    });
    return res.json();
  },

  updatePost: async (id, post) => {
    const res = await fetch(`${BASE_URL}/${id}`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(post),
    });
    return res.json();
  },

  deletePost: async (id) => {
    await fetch(`${BASE_URL}/${id}`, { method: "DELETE" });
  },
};
```

  ##### `src/PostForm.js`

```jsx
import { useState, useEffect } from "react";

function PostForm({ onSubmit, editingPost, onCancel, error }) {
  const [title, setTitle] = useState("");
  const [body, setBody] = useState("");

  useEffect(() => {
    if (editingPost) {
      setTitle(editingPost.title);
      setBody(editingPost.body);
    } else {
      setTitle("");
      setBody("");
    }
  }, [editingPost]);

  const handleSubmit = (e) => {
    e.preventDefault();
    if (!title.trim() || !body.trim()) return;
    onSubmit({ title, body, userId: 1 });
    setTitle("");
    setBody("");
  };

  return (
    <form onSubmit={handleSubmit}>
      <h2>{editingPost ? "Edit Post" : "New Post"}</h2>
      {error && <p style={{ color: "red" }}>{error}</p>}
      <input
        placeholder="Title"
        value={title}
        onChange={(e) => setTitle(e.target.value)}
      />
      <br />
      <textarea
        placeholder="Body"
        value={body}
        onChange={(e) => setBody(e.target.value)}
        rows={4}
      />
      <br />
      <button type="submit">{editingPost ? "Update" : "Create"}</button>
      {editingPost && (
        <button type="button" onClick={onCancel}>Cancel</button>
      )}
    </form>
  );
}

export default PostForm;
```
##### `src/PostList.js`

```jsx
import { Link } from "react-router-dom";

function PostList({ posts, onEdit, onDelete }) {
  if (posts.length === 0) {
    return <p>No posts yet. Create one above!</p>;
  }

  return (
    <ul>
      {posts.map((post) => (
        <li key={post.id}>
          <Link to={`/posts/${post.id}`}><strong>{post.title}</strong></Link>
          <p>{post.body}</p>
          <button onClick={() => onEdit(post)}>Edit</button>
          <button onClick={() => onDelete(post.id)}>Delete</button>
        </li>
      ))}
    </ul>
  );
}

export default PostList;
```

##### `src/PostDetail.js`

```jsx
import { useState, useEffect } from "react";
import { useParams, useNavigate, Link } from "react-router-dom";

function PostDetail() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [post, setPost] = useState(null);
  const [error, setError] = useState(null);

  useEffect(() => {
    fetch(`https://jsonplaceholder.typicode.com/posts/${id}`)
      .then((res) => {
        if (!res.ok) throw new Error("Post not found");
        return res.json();
      })
      .then(setPost)
      .catch((err) => setError(err.message));
  }, [id]);

  const handleDelete = async () => {
    try {
      await fetch(`https://jsonplaceholder.typicode.com/posts/${id}`, { method: "DELETE" });
      navigate("/");
    } catch (err) {
      setError(err.message);
    }
  };

  if (error) return <p style={{ color: "red" }}>{error}</p>;
  if (!post) return <p>Loading...</p>;

  return (
    <div>
      <Link to="/">&larr; Back to all posts</Link>
      <h1>{post.title}</h1>
      <p>{post.body}</p>
      <button onClick={handleDelete}>Delete</button>
    </div>
  );
}

export default PostDetail;
```

##### `src/App.js`

```jsx
import { useState, useEffect } from "react";
import { BrowserRouter, Routes, Route } from "react-router-dom";
import { api } from "./api";
import PostForm from "./PostForm";
import PostList from "./PostList";
import PostDetail from "./PostDetail";

function HomePage() {
  const [posts, setPosts] = useState([]);
  const [editingPost, setEditingPost] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    api.getPosts()
      .then(setPosts)
      .catch((err) => setError(err.message))
      .finally(() => setLoading(false));
  }, []);

  const handleSubmit = async (postData) => {
    setError(null);
    try {
      if (editingPost) {
        const updated = await api.updatePost(editingPost.id, postData);
        setPosts(posts.map((p) => (p.id === editingPost.id ? { ...p, ...updated } : p)));
        setEditingPost(null);
      } else {
        const created = await api.createPost(postData);
        setPosts([{ ...created, id: Date.now() }, ...posts]);
      }
    } catch (err) {
      setError(err.message);
    }
  };

  const handleDelete = async (id) => {
    setError(null);
    try {
      await api.deletePost(id);
      setPosts(posts.filter((p) => p.id !== id));
    } catch (err) {
      setError(err.message);
    }
  };

  const handleEdit = (post) => {
    setEditingPost(post);
  };

  if (loading) return <p>Loading posts...</p>;

  return (
    <div>
      <h1>Posts Manager</h1>
      <PostForm
        onSubmit={handleSubmit}
        editingPost={editingPost}
        onCancel={() => setEditingPost(null)}
        error={error}
      />
      <PostList posts={posts} onEdit={handleEdit} onDelete={handleDelete} />
    </div>
  );
}

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<HomePage />} />
        <Route path="/posts/:id" element={<PostDetail />} />
      </Routes>
    </BrowserRouter>
  );
}

export default App;
```


```
npx create-react-app posts-app
cd posts-app && npm install react-router-dom && npm run start
```